﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using FistVR;
using Sodalite.Api;
using Sodalite.Patcher;
using Sodalite.UiWidgets;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

namespace Sodalite
{
	[BepInPlugin("nrgill28.Sodalite", "Sodalite", "0.3.0")]
	[BepInProcess("h3vr.exe")]
	public class Sodalite : BaseUnityPlugin, ILogListener
	{
		// Private fields
		private readonly List<LogEventArgs> _logEvents;
		private readonly LockablePanel _logPanel;
		private BepInExLogPanel? _logPanelComponent;
		private static ManualLogSource? _logger;

		// Static stuff
		private static readonly string ResourcesPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "res");
		internal static ManualLogSource StaticLogger => _logger ?? throw new InvalidOperationException("Cannot get logger before the behaviour is initialized!");

		public Sodalite()
		{
			// Patch a call to GetTypes
			On.UnityEngine.Rendering.PostProcessing.PostProcessManager.ReloadBaseTypes += PostProcessManagerOnReloadBaseTypes;

			// Set our logger so it's accessible from anywhere
			_logger = Logger;

			// Register ourselves as the new log listener and try to grab what's already been captured
			BepInEx.Logging.Logger.Listeners.Add(this);
			// Grab the captured logs from the buffer and dispose it.
			_logEvents = SodalitePatcher.LogBuffer.LogEvents;
			SodalitePatcher.LogBuffer.Dispose();

			// Make a new LockablePanel for the console panel
			_logPanel = new LockablePanel();
			_logPanel.Configure += ConfigureLogPanel;
			_logPanel.TextureOverride = Utilities.LoadTextureFromBytes(Assembly.GetExecutingAssembly().GetResource("LogPanel.png"));
			H3Api.WristMenu.Buttons.Add(new WristMenuButton("Spawn Log Panel", int.MaxValue, SpawnLogPanel));
		}

		private void Start()
		{
			// Pull the button sprite and font for our use later
			Transform button = H3Api.WristMenu!.Instance!.OptionsPanelPrefab.transform.Find("OptionsCanvas_0_Main/Canvas/Label_SelectASection/Button_Option_1_Locomotion");
			WidgetStyle.DefaultButtonSprite = button.GetComponent<Image>().sprite;
			WidgetStyle.DefaultTextFont = button.GetChild(0).GetComponent<Text>().font;
		}

		#region Utility Panel (Widgets test)

		/*
		private static void ConfigureUtilityPanel(GameObject panel)
		{
			GameObject canvas = panel.transform.Find("OptionsCanvas_0_Main/Canvas").gameObject;
			UiWidget.CreateAndConfigureWidget(canvas, (GridLayoutWidget widget) =>
			{
				// Fill our parent and set pivot to top middle
				widget.RectTransform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
				widget.RectTransform.localPosition = Vector3.zero;
				widget.RectTransform.anchoredPosition = Vector2.zero;
				widget.RectTransform.sizeDelta = new Vector2(37f / 0.07f, 24f / 0.07f);
				widget.RectTransform.pivot = new Vector2(0.5f, 1f);

				// Adjust our grid settings
				widget.LayoutGroup.cellSize = new Vector2(171, 50);
				widget.LayoutGroup.spacing = Vector2.one * 4;
				widget.LayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
				widget.LayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
				widget.LayoutGroup.childAlignment = TextAnchor.UpperCenter;
				widget.LayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
				widget.LayoutGroup.constraintCount = 3;

				widget.AddChild((ButtonWidget button) => button.ButtonText.text = "Button 1!");
				widget.AddChild((ButtonWidget button) => button.ButtonText.text = "Mod Configs");
				widget.AddChild((ButtonWidget button) => button.ButtonText.text = "Another button");
				widget.AddChild((ButtonWidget button) => button.ButtonText.text = "Another button 2");
				widget.AddChild((ButtonWidget button) => button.ButtonText.text = "Another button 3");
			});
		}

		private void SpawnUtilityPanel()
		{
			if (_api.WristMenu is null || !_api.WristMenu) return;
			GameObject panel = _utilityPanel.GetOrCreatePanel();
			_api.WristMenu.m_currentHand.RetrieveObject(panel.GetComponent<FVRPhysicalObject>());
		}
		*/

		#endregion

		#region Log Panel Stuffs

		// Wrist menu button callback. Gets our panel instance and makes the hand retrieve it.
		private void SpawnLogPanel()
		{
			FVRWristMenu? wristMenu = H3Api.WristMenu.Instance;
			if (wristMenu is null || !wristMenu) return;
			GameObject panel = _logPanel.GetOrCreatePanel();
			wristMenu.m_currentHand.RetrieveObject(panel.GetComponent<FVRPhysicalObject>());
		}

		private void ConfigureLogPanel(GameObject panel)
		{
			Transform canvasTransform = panel.transform.Find("OptionsCanvas_0_Main/Canvas");
			_logPanelComponent = panel.AddComponent<BepInExLogPanel>();
			_logPanelComponent.CreateWithExisting(this, canvasTransform.gameObject, _logEvents);
		}

		void ILogListener.LogEvent(object sender, LogEventArgs eventArgs)
		{
			_logEvents.Add(eventArgs);
			if (_logPanelComponent && _logPanelComponent is not null) _logPanelComponent.LogEvent();
		}

		void IDisposable.Dispose()
		{
			BepInEx.Logging.Logger.Listeners.Remove(this);
			_logEvents.Clear();
		}

		#endregion

		// Patch for a location in the game's code that uses an Assembly.GetTypes() call. This could be an IL thing but I can't be bothered to do that right now.
		private static void PostProcessManagerOnReloadBaseTypes(On.UnityEngine.Rendering.PostProcessing.PostProcessManager.orig_ReloadBaseTypes orig, PostProcessManager self)
		{
			self.CleanBaseTypes();
			IEnumerable<Type> enumerable = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => from t in a.GetTypesSafe()
				where t.IsSubclassOf(typeof(PostProcessEffectSettings)) && t.IsDefined(typeof(PostProcessAttribute), false)
				select t);
			foreach (Type type in enumerable)
			{
				self.settingsTypes.Add(type, type.GetAttribute<PostProcessAttribute>());
				PostProcessEffectSettings postProcessEffectSettings = (PostProcessEffectSettings) ScriptableObject.CreateInstance(type);
				postProcessEffectSettings.SetAllOverridesTo(true, false);
				self.m_BaseSettings.Add(postProcessEffectSettings);
			}
		}
	}
}
