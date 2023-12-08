using Godot;
using System;
using System.Collections.Generic;

public partial class sound_controller : Node3D
{
	Dictionary<string, AudioStreamOggVorbis> sounds = new Dictionary<string, AudioStreamOggVorbis>();

	// AudioStreamOggVorbis CardDraw = ResourceLoader.Load("res://MVC/View/Audio/SFX/CardDraw.ogg") as AudioStreamOggVorbis;
	// AudioStreamOggVorbis CardMove = ResourceLoader.Load("res://MVC/View/Audio/SFX/CardMove.ogg") as AudioStreamOggVorbis;
	// AudioStreamOggVorbis CardPlace = ResourceLoader.Load("res://MVC/View/Audio/SFX/CardPlace.ogg") as AudioStreamOggVorbis;
	// AudioStreamOggVorbis PlayerAttack = ResourceLoader.Load("res://MVC/View/Audio/SFX/PlayerAttack.ogg") as AudioStreamOggVorbis;

	

	
	[Export] private AudioStreamPlayer player1, player2, player3;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		sounds.Add("CardDraw", ResourceLoader.Load("res://MVC/View/Audio/SFX/CardDraw.ogg") as AudioStreamOggVorbis);
		sounds.Add("CardMove", ResourceLoader.Load("res://MVC/View/Audio/SFX/CardMove.ogg") as AudioStreamOggVorbis);
		sounds.Add("CardPlace", ResourceLoader.Load("res://MVC/View/Audio/SFX/CardPlace.ogg") as AudioStreamOggVorbis);
		sounds.Add("PlayerAttack", ResourceLoader.Load("res://MVC/View/Audio/SFX/PlayerAttack.ogg") as AudioStreamOggVorbis);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}

	public void Play(string soundName) {
		GetNode<AudioStreamPlayer>("AudioStreamPlayer").Stream = sounds[soundName];
		GetNode<AudioStreamPlayer>("AudioStreamPlayer").Play();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("CamControl"))
		{
			Play("PlayerAttack");
		}
	}
}
