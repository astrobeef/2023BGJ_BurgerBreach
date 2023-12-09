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

	[Export] private AudioStreamPlayer[] SFXStreams;
	[Export] private AudioStreamPlayer[] MusicStreams;

	public override void _Ready()
	{
		LoadSoundsFromFolder("res://MVC/View/Audio/SFX");
		LoadSoundsFromFolder("res://MVC/View/Audio/Music");

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
				if (!dir.CurrentIsDir()) {
					if (fileName.ToLower().EndsWith(".ogg")) {
						string key = fileName.Substring(0, fileName.Length - 4);
						sounds.Add(key, ResourceLoader.Load(path + "/" + fileName) as AudioStreamOggVorbis);

					} else if (fileName.ToLower().EndsWith(".wav")) {
						string key = fileName.Substring(0, fileName.Length - 4);
						sounds.Add(key, ResourceLoader.Load(path + "/" + fileName) as AudioStreamWav);

					} else if (fileName.EndsWith(".mp3")) {
						string key = fileName.Substring(0, fileName.Length - 4);
						sounds.Add(key, ResourceLoader.Load(path + "/" + fileName) as AudioStreamMP3);
					}
				}

				fileName = dir.GetNext();
			}

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
	// 		PlayMusic("Friday");
	// 	}
		
	// 	if (@event.IsActionPressed("Debug2"))
	// 	{
	// 		StopMusic("Friday");
	// 	}
	// }
}
