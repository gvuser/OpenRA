#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using System;

namespace OpenRA.GameRules
{
	public class ServerSettings
	{
		public string LastServerTitle = "OpenRA Game";
		public int ListenPort = 1234;
		public int ExternalPort = 1234;
		public bool AdvertiseOnline = true;
		public string MasterServer = "http://open-ra.org/master/";
		public bool AllowCheats = false;
	}
	
	public class DebugSettings
	{
		public bool PerfDebug = false;
		public bool RecordSyncReports = true;
		public bool ShowGameTimer = true;
		public bool UnitDebug = false;
	}
	
	public class GraphicSettings
	{
		public WindowMode WindowMode = WindowMode.PseudoFullscreen;
		public int2 FullscreenSize = new int2(Screen.PrimaryScreen.Bounds.Width,Screen.PrimaryScreen.Bounds.Height);
		public int2 WindowedSize = new int2(1024,768);
		public readonly int2 MinResolution = new int2(800, 600);
	}
	
	public class SoundSettings
	{
		public float SoundVolume = 0.5f;
		public float MusicVolume = 0.5f;
		public float VideoVolume = 0.5f;
		public bool MusicPlayer = false;
	}
	
	public class PlayerSettings
	{
		public string PlayerName = "Newbie";
		public Color PlayerColor1 = Color.FromArgb(255,160,238);
		public Color PlayerColor2 = Color.FromArgb(68,0,56);
	}
	
	public class GeneralSettings
	{
		// Behaviour settings
        public bool ViewportEdgeScroll = true;
        public bool InverseDragScroll = false;
		
		// Internal game settings
		public int Timestep = 40;
		public int SheetSize = 2048;
		
		// External game settings
		public string LastServer = "localhost:1234";

		public string[] InitialMods = { "ra" };
	}
	
	public class Settings
	{
		string SettingsFile;
		
		public PlayerSettings Player = new PlayerSettings();
		public GeneralSettings General = new GeneralSettings();
		public SoundSettings Sound = new SoundSettings();
		public GraphicSettings Graphics = new GraphicSettings();
		public ServerSettings Server = new ServerSettings();
		public DebugSettings Debug = new DebugSettings();
		
		Dictionary<string, object> Sections;
		public Settings(Arguments args)
		{			
			SettingsFile = Game.SupportDir + "settings.yaml";
			Sections = new Dictionary<string, object>()
			{
				{"Player", Player},
				{"General", General},
				{"Sound", Sound},
				{"Graphics", Graphics},
				{"Server", Server},
				{"Debug", Debug}
			};
			
			// Override fieldloader to ignore invalid entries
			var err1 = FieldLoader.UnknownFieldAction;
			var err2 = FieldLoader.InvalidValueAction;
			
			FieldLoader.UnknownFieldAction = (s,f) =>
			{
				System.Console.WriteLine( "Ignoring unknown field `{0}` on `{1}`".F( s, f.Name ) );
			};
			
			if (File.Exists(SettingsFile))
			{
				System.Console.WriteLine("Loading settings file {0}",SettingsFile);
				var yaml = MiniYaml.FromFile(SettingsFile);
				
				foreach (var kv in Sections)
					LoadSectionYaml(yaml[kv.Key], kv.Value);
			}
			
			// Override with commandline args
			foreach (var kv in Sections)
				foreach (var f in kv.Value.GetType().GetFields())
					if (args.Contains(kv.Key+"."+f.Name))
						OpenRA.FileFormats.FieldLoader.LoadField( kv.Value, f.Name, args.GetValue(kv.Key+"."+f.Name, "") );
			
			FieldLoader.UnknownFieldAction = err1;
			FieldLoader.InvalidValueAction = err2;
		}
		
		public void Save()
		{
			Dictionary<string, MiniYaml> root = new Dictionary<string, MiniYaml>();
			foreach (var kv in Sections)
				root.Add(kv.Key, SectionYaml(kv.Value));
			
			root.WriteToFile(SettingsFile);
		}
		
		MiniYaml SectionYaml(object section)
		{
			return FieldSaver.SaveDifferences(section, Activator.CreateInstance(section.GetType()));
		}
		
		void LoadSectionYaml(MiniYaml yaml, object section)
		{
			object defaults = Activator.CreateInstance(section.GetType());
			FieldLoader.InvalidValueAction = (s,t,f) =>
			{
				object ret = defaults.GetType().GetField(f).GetValue(defaults);
				System.Console.WriteLine("FieldLoader: Cannot parse `{0}` into `{2}:{1}`; substituting default `{3}`".F(s,t.Name,f,ret) );
				return ret;
			};
			
			FieldLoader.Load(section, yaml);
		}
	}
}
