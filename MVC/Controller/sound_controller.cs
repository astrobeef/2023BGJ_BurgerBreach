using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public partial class sound_controller : Node3D
{
	public const string SFX_CARD_DRAW_NAME = "CardDraw_01";
	public const string SFX_CARD_MOVE_NAME = "CardMove";
	public const string SFX_CARD_PLACE_NAME = "CardPlace";
	public const string SFX_PLAYER_ATTACK_NAME = "PlayerAttack";

	Dictionary<string, AudioStream> sounds = new Dictionary<string, AudioStream>();

	// private AudioStreamPlayer SFX0, SFX1, SFX2, SFX3, SFX4, SFX5;
	// private AudioStreamPlayer Music0, Music1;
	
	[Export] private AudioStreamPlayer[] SFXStreams;
	[Export] private AudioStreamPlayer[] MusicStreams;

	public override void _Ready()
	{
		LoadSoundsFromFolder("res://MVC/View/Audio/SFX");
		LoadSoundsFromFolder("res://MVC/View/Audio/Music");
		LoadSoundsFromFolder("res://MVC/View/Audio/");
		
		// SFX0 = GetNode<AudioStreamPlayer>("AudioStreamPlayer0");
		// SFX1 = GetNode<AudioStreamPlayer>("AudioStreamPlayer1");
		// SFX2 = GetNode<AudioStreamPlayer>("AudioStreamPlayer2");
		// SFX3 = GetNode<AudioStreamPlayer>("AudioStreamPlayer3");
		// SFX4 = GetNode<AudioStreamPlayer>("AudioStreamPlayer4");
		// SFX5 = GetNode<AudioStreamPlayer>("AudioStreamPlayer5");
		// Music0 = GetNode<AudioStreamPlayer>("AudioStreamPlayer6");
		// Music1 = GetNode<AudioStreamPlayer>("AudioStreamPlayer7");

		var async = async () =>
		{
			await System.Threading.Tasks.Task.Delay(10);
			Play(SFX_CARD_MOVE_NAME);
			// Play("Friday");
		};
		async.Invoke();
	}

	public override void _Process(double delta)
	{
		
	}

	public AudioStreamPlayer Play(string soundName) {
		return PlaySFX(soundName);
	}

	public bool Stop(string soundName) {
		if (!IsSoundLoaded(soundName)) return false;
		
		if (StopSFX(soundName)) return true;
		if (StopMusic(soundName)) return true;

		return false;
	}

	public AudioStreamPlayer PlaySFX(string soundName) {
		if (!IsSoundLoaded(soundName)) return null;

		for (int i = 0; i < SFXStreams.Length; i++) {
			if (!SFXStreams[i].Playing) {
				SFXStreams[i].Stream = sounds[soundName];
				SFXStreams[i].Play();
				return SFXStreams[i];
			}
		}

		SFXStreams[0].Stream = sounds[soundName];
		SFXStreams[0].Play();
		return SFXStreams[0];
	}
	
	// public AudioStreamPlayer PlaySFX(string soundName) {
	// 	if (!IsSoundLoaded(soundName)) return null;

	// 	if (!SFX0.Playing) {
	// 		SFX0.Stream = sounds[soundName];
	// 		SFX0.Play();
	// 		return SFX0;

	// 	} else if (!SFX1.Playing) {
	// 		SFX1.Stream = sounds[soundName];
	// 		SFX1.Play();
	// 		return SFX1;
			
	// 	} else if (!SFX2.Playing) {
	// 		SFX2.Stream = sounds[soundName];
	// 		SFX2.Play();
	// 		return SFX2;
			
	// 	} else if (!SFX3.Playing) {
	// 		SFX3.Stream = sounds[soundName];
	// 		SFX3.Play();
	// 		return SFX3;
			
	// 	} else if (!SFX4.Playing) {
	// 		SFX4.Stream = sounds[soundName];
	// 		SFX4.Play();
	// 		return SFX4;
			
	// 	} else if (!SFX5.Playing) {
	// 		SFX5.Stream = sounds[soundName];
	// 		SFX5.Play();
	// 		return SFX5;
			
	// 	}

	// 	SFX0.Stream = sounds[soundName];
	// 	SFX0.Play();
	// 	return SFX0;
	// }

	public bool StopSFX(string soundName) {
		if (!IsSoundLoaded(soundName)) return false;

		for (int i = 0; i < SFXStreams.Length; i++) {
			if (SFXStreams[i].Stream == sounds[soundName]) {
				SFXStreams[i].Stop();
				return true;
			}
		}

		return false;
	}

	public AudioStreamPlayer PlayMusic(string soundName) {
		if (!IsSoundLoaded(soundName)) return null;

		for (int i = 0; i < MusicStreams.Length; i++) {
			if (!MusicStreams[i].Playing) {
				MusicStreams[i].Stream = sounds[soundName];
				MusicStreams[i].Play();
				return MusicStreams[i];
			}
		}

		MusicStreams[0].Stream = sounds[soundName];
		MusicStreams[0].Play();
		return MusicStreams[0];
	}

	public bool StopMusic(string soundName) {
		if (!IsSoundLoaded(soundName)) return false;

		for (int i = 0; i < SFXStreams.Length; i++) {
			if (MusicStreams[i].Stream == sounds[soundName]) {
				MusicStreams[i].Stop();
				return true;
			}
		}

		return false;
	}

	public void LoadSoundsFromFolder(string path)
	{
		using var dir = DirAccess.Open(path);
		if (dir != null)
		{
			dir.ListDirBegin();
			string fileName = dir.GetNext();
			while (fileName != "")
			{
				// Real loader that doesnt work outside of editor for some unknown reason

				// if (!dir.CurrentIsDir()) {
				// 	if (fileName.ToLower().EndsWith(".ogg")) {
				// 		string key = fileName.Substring(0, fileName.Length - 4);
				// 		sounds.Add(key, ResourceLoader.Load(path + "/" + fileName) as AudioStreamOggVorbis);

				// 	} else if (fileName.ToLower().EndsWith(".wav")) {
				// 		string key = fileName.Substring(0, fileName.Length - 4);
				// 		sounds.Add(key, ResourceLoader.Load(path + "/" + fileName) as AudioStreamWav);

				// 	} else if (fileName.EndsWith(".mp3")) {
				// 		string key = fileName.Substring(0, fileName.Length - 4);
				// 		sounds.Add(key, ResourceLoader.Load(path + "/" + fileName) as AudioStreamMP3);
				// 	}
				// }

				fileName = dir.GetNext();
			}

			// Scuffed Loader
			sounds["battleMusic"] = SFXStreams[0].Stream;
			sounds["CardDraw_01"] = SFXStreams[1].Stream;
			sounds["CardMove"] = SFXStreams[2].Stream;
			sounds["CardPlace"] = SFXStreams[3].Stream;
			sounds["PlayerAttack"] = SFXStreams[4].Stream;
			sounds["PlayerBuff"] = SFXStreams[5].Stream;

		} else {
			GD.Print("An error occurred when trying to access the path.");
		}
	}

	public bool IsSoundLoaded(string soundName) {
		if (!sounds.ContainsKey(soundName)) {
			GD.Print("No sound with this name has been loaded");
			return false;
		}

		return true;
	}

	// public override void _Input(InputEvent @event)
	// {
	// 	if (@event.IsActionPressed("Debug1"))
	// 	{
	// 		PlaySFX(SFX_PLAYER_ATTACK_NAME);
	// 	}

	// 	if (@event.IsActionPressed("Debug2"))
	// 	{
	// 		PlaySFX(SFX_CARD_MOVE_NAME);
	// 	}

	// 	if (@event.IsActionPressed("Debug3"))
	// 	{
	// 		PlaySFX(SFX_CARD_PLACE_NAME);
	// 	}
	// }
}
