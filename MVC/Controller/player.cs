using AxialCS;
using Deck;
using Godot;
using Model;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Utility;
using static DialogueUtility;

public partial class player : Node3D
{
	[Export(PropertyHint.Layers3DPhysics)] public uint CameraHitLayers;
	[Export] private Camera3D camera;

	[Export] private Vector3 top_down_position = new Vector3(0, 1.3f, 0.00f);
	[Export] private Vector3 top_down_rotation = new Vector3(-90, 0, 0);
	[Export] private Vector3 perspective_position = new Vector3(0f, 0.9f, 0.7f);
	[Export] private Vector3 perspective_rotation = new Vector3(-28, 0, 0);
	[Export] private float transition_time = 0.4f;

	private bool top_down = true;

	private Hit3D _camHoverHit;
	public Hit3D CamHoverHit => _camHoverHit;

	private Hit3D _camClickHit;
	public Hit3D CamClickHit => _camClickHit;

	public Action<Hit3D> OnCamHoverNewHit;
	public Action<Hit3D> OnCamHoverOff;
	public Action<Hit3D> OnCamHoverUpdate;

	public Action<Hit3D> OnCamClickNewHit;
	public Action<Hit3D> OnCamClickOff;
	public Action<Hit3D> OnCamClickUpdate;

	public Node3D selectedObject;
	public Action<Node3D> OnObjectSelected;
	public Action<Node3D> OnObjectDeselected;

	/// <summary>
	/// Fires when the player has entered/exited attack mode (true if enter, false if exit)
	/// </summary>
	public Action<bool> OnPlayerAttackMode;

	[Export]
	public Node3D DEBUG_selectMarker;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		camera = GetChild(0) as Camera3D;

		main.Instance.gameModel.OnTurnEnd += OnTurnEnd;
		main.Instance.gameModel.OnAwaitTurnActions += OnAwaitTurnActions;

		OnObjectSelected += DEBUG_OnObjectSelected;
		main.Instance.gameModel.OnUnitMove += DEBUG_OnObjectSelected;
		OnObjectDeselected += DEBUG_OnObjectDeselected;


		var async = async () =>
		{
			await Task.Delay(500);
			SwitchToPerspective();
		};

		async.Invoke();
	}

	private void OnAwaitTurnActions(int turnPlayerIndex, int turnCounter)
	{
		if (turnPlayerIndex == 0)
		{
			DeselectObject();
			playerIntention = PlayerIntention.Open;
		}
	}

	private void OnTurnEnd(int endTurnPlayerIndex, int turnCounter)
	{
		if (endTurnPlayerIndex == 0)
		{
			DeselectObject();
			playerIntention = PlayerIntention.DISABLED;
		}
	}

	private void DeselectObject()
	{
		if (selectedObject != null)
		{
			OnObjectDeselected?.Invoke(selectedObject);
			switch (selectedObject)
			{
				case Card3D card3D:
					card3D.OnObjectDeselected();
					break;
				case Unit3D unit3D:
					unit3D.OnObjectDeselected();
					break;
				case Hex3D hex3D:
					hex3D.OnObjectDeselected();
					break;
				default:
					break;
			}

			selectedObject = null;
		}
	}

	private void DEBUG_OnObjectSelected(Node3D obj)
	{
		if (obj != null)
			DEBUG_selectMarker.GlobalPosition = obj.GlobalPosition + Vector3.Up * 0.02f;
	}

	private void DEBUG_OnObjectSelected(Axial position, Unit unit)
	{
		var async = async () =>
		{
			await System.Threading.Tasks.Task.Delay(10);

			if (unit != null && unit == (selectedObject as Unit3D)?.unit)
				DEBUG_selectMarker.GlobalPosition = selectedObject.GlobalPosition + Vector3.Up * 0.02f;
			else
				GD.PrintErr($"Unit {unit} is not equal to selected object {selectedObject as Unit3D}");
		};

		async.Invoke();
	}

	private void DEBUG_OnObjectDeselected(Node3D obj)
	{
		DEBUG_selectMarker.GlobalPosition = Vector3.Zero;
	}

	bool _leftMouseClicked = false;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (_leftMouseClicked)
			_leftMouseClicked = Input.IsMouseButtonPressed(MouseButton.Left);

		if (Input.IsMouseButtonPressed(MouseButton.Left))
		{
			if (!_leftMouseClicked)
			{
				_leftMouseClicked = true;

				if (Raycasting.RayCast(camera, CameraHitLayers, out Hit3D hit))
				{
					// If it is a new hit
					if (_camClickHit != hit)
					{
						Hit3D previousHit = _camClickHit;
						_camClickHit = hit;

						// If the colliders are NOT the same (meaning this hit has hit a new object)
						if (_camClickHit.collider != previousHit.collider)
						{
							OnCamClickOff?.Invoke(previousHit);
							OnCamClickNewHit?.Invoke(_camClickHit);
						}
						// Else the hit is at a different position, but on the same object
						else
						{
							OnCamClickUpdate?.Invoke(_camClickHit);
						}
					}
					// Else the hit is at the same point on the same object
					else
					{
						OnCamClickUpdate?.Invoke(_camClickHit);
					}
				}
				else
				{
					if (_camClickHit != Hit3D.EMPTY)
					{
						Hit3D previousHit = _camClickHit;
						_camClickHit = Hit3D.EMPTY;

						OnCamClickOff?.Invoke(previousHit);
					}
				}
			}
		}
	}

	int wait = 0, waitThresh = 5;

	public override void _PhysicsProcess(double delta)
	{
		// Only fire method if an event is listening
		if (OnCamHoverNewHit != null || OnCamHoverOff != null)
		{
			if (camera != null)
			{
				wait++;
				if (wait % waitThresh == 0)
				{
					if (Raycasting.RayCast(camera, CameraHitLayers, out Hit3D hit))
					{
						// If it is a new hit
						if (_camHoverHit != hit)
						{
							Hit3D previousHit = _camHoverHit;
							_camHoverHit = hit;

							// If the colliders are NOT the same (meaning this hit has hit a new object)
							if (_camHoverHit.collider != previousHit.collider)
							{
								OnCamHoverOff?.Invoke(previousHit);

								OnCamHoverNewHit?.Invoke(_camHoverHit);
							}
							// Else the hit is at a different position, but on the same object
							else
							{
								OnCamHoverUpdate?.Invoke(_camHoverHit);
							}
						}
					}
					else
					{
						if (_camHoverHit != Hit3D.EMPTY)
						{
							Hit3D previousHit = _camHoverHit;
							_camHoverHit = Hit3D.EMPTY;

							OnCamHoverOff?.Invoke(previousHit);
						}
					}
				}
			}
			else
			{
				GD.PrintErr("camera null");
			}
		}
	}

	public void SwitchToTopDown()
	{
		if (!top_down)
		{
			Tween tween = GetTree().CreateTween();
			tween.SetParallel(true);
			tween.TweenProperty(this, "position", top_down_position, transition_time);
			tween.TweenProperty(this, "rotation_degrees", top_down_rotation, transition_time);
			top_down = true;
		}
	}

	public void SwitchToPerspective()
	{
		if (top_down)
		{
			Tween tween = GetTree().CreateTween();
			tween.SetParallel(true);
			tween.TweenProperty(this, "position", perspective_position, transition_time);
			tween.TweenProperty(this, "rotation_degrees", perspective_rotation, transition_time);
			top_down = false;
		}
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("CamControl"))
		{
			if (top_down)
			{
				SwitchToPerspective();
			}
			else
			{
				SwitchToTopDown();
			}
		}

		if (@event.IsActionPressed("EnableAttack"))
		{
			Unit3D selectedUnit3D = selectedObject as Unit3D;

			if (selectedUnit3D != null)
			{
				if (playerIntention == PlayerIntention.UnitMove && selectedUnit3D.unit.CanAttack())
				{
					playerIntention = PlayerIntention.UnitAttack;
					OnPlayerAttackMode(true);

					GD.Print($"Changed player intention to {playerIntention}");
				}
				else if (playerIntention == PlayerIntention.UnitAttack)
				{
					playerIntention = PlayerIntention.UnitMove;
					OnPlayerAttackMode(false);

					GD.Print($"Changed player intention to {playerIntention}");
				}
			}
			else
			{
				GD.PrintErr("Cannot enable attack because no unit is selected");
			}
		}
	}

	public enum PlayerIntention { DISABLED, Open, PlaceCard, UnitMove, UnitAttack };
	public PlayerIntention playerIntention = PlayerIntention.DISABLED;

	public bool HandleObjectClicked(Node3D node3D)
	{
		if (playerIntention == PlayerIntention.DISABLED || node3D == selectedObject && node3D != null)
		{
			if(playerIntention == PlayerIntention.DISABLED && node3D != null)
			{
				if(main.Instance.gameModel.TurnPlayerIndex == 0)
				{
					main.Instance.DS.QueueMessage(true, $"You need to {C_(GREEN)}draw a card{C_()} before you can play. Sorry if that's confusing, {C_(ORANGE)}John B. Modeson{C_()} is still working on the details of the game.");
				}
				else
				{
					main.Instance.DS.QueueMessage(true, $"Are you trying to play on my turn? I wish it worked that way... I'll talk to the {C_(ORANGE)}Sous Chef{C_()} about that.");
				}
			}
			GD.Print($"Not handling clicked object because either player intention is disabled({playerIntention == PlayerIntention.DISABLED}) or this Node3D is already selected({node3D == selectedObject})");
			GD.PrintErr("Play input error SFX here");
			return false;
		}

		switch (node3D)
		{
			// When something irrelevant is selected
			case null:
				{
					switch (playerIntention)
					{
						case PlayerIntention.Open:
							{
								// If the player is in top down and their intention is open, then we assume that they want to switch back to perspective
								if (top_down)
									SwitchToPerspective();
								break;
							}
						case PlayerIntention.PlaceCard:
							{
								// If the player currently has a card selected and they've clicked off the board, then we assume they want to deselect the card and return to their deck
								Card3D selectedCard3D = selectedObject as Card3D;
								

								if (selectedCard3D != null && selectedCard3D.OnObjectDeselected())
								{
									main.Instance.DS.QueueMessage(false, $"You decided you didn't want to play your {C_(ORANGE)}{selectedCard3D.card.NAME}{C_()}, huh?");

									OnObjectDeselected?.Invoke(selectedCard3D);

									if (top_down)
										SwitchToPerspective();

									playerIntention = PlayerIntention.Open;
									selectedObject = null;
								}
								else throw new Exception($"Could not select \"{selectedCard3D?.Name}\". Either no card is selected ({selectedCard3D == null}) OR it failed to deselect");

								break;
							}
						case PlayerIntention.UnitMove:
						case PlayerIntention.UnitAttack:
							{
								// If the player currently has a unit selected and they've clicked off the board, then we assume they want to deselect the unit but remain in top-down (if they are in top-down)
								Unit3D selectedUnit3D = selectedObject as Unit3D;

								if (selectedUnit3D != null && selectedUnit3D.OnObjectDeselected())
								{
									main.Instance.DS.QueueMessage(false, $"You've gone and pulled the tomato right out the buns. I thought you were gonna fry me up with your {C_(ORANGE)}{selectedUnit3D.unit.card.NAME}{C_()}");

									OnObjectDeselected?.Invoke(selectedUnit3D);

									playerIntention = PlayerIntention.Open;
									selectedObject = null;
								}
								else throw new Exception($"Could not select \"{selectedUnit3D?.Name}\". Either no card is selected ({selectedUnit3D == null}) OR it failed to deselect");

								break;
							}
					}
					break;
				}
			case Card3D card3D:
				{
					

					DisplayMessageOnCardSelect(card3D);




					switch (playerIntention)
					{
						case PlayerIntention.Open:
							{
								/*
								* Assumption is that if the player clicks on a card they want to select it to place it.
								*/

								Card3D cardToSelect = card3D;

								// Select this card
								if (cardToSelect != null && cardToSelect.OnObjectSelected())
								{
									OnObjectSelected?.Invoke(cardToSelect);
									// Set intention to 'PlaceCard'
									playerIntention = PlayerIntention.PlaceCard;
									selectedObject = cardToSelect;
									GD.Print($"Selected card {cardToSelect.card.NAME}. Changed player intention to {playerIntention}");
								}
								else throw new Exception($"Could not select \"{card3D?.Name}\".");
								break;
							}
						case PlayerIntention.PlaceCard:
							{
								/*
								* Assumption is that, regardless of current intention, if the player clicks on a card they want to select it to place it.
								*/

								Card3D selectedCard3D = selectedObject as Card3D;
								Card3D cardToSelect = card3D;

								// Deselect current object (should be a card based on state)
								if (selectedCard3D != null && selectedCard3D.OnObjectDeselected())
								{
									OnObjectDeselected?.Invoke(selectedObject);
									// Select this card
									if (cardToSelect != null && cardToSelect.OnObjectSelected())
									{
										OnObjectSelected?.Invoke(cardToSelect);
										// Set intention to 'PlaceCard'
										playerIntention = PlayerIntention.PlaceCard;
										selectedObject = cardToSelect;
										GD.Print($"Selected card {cardToSelect.card.NAME}. Changed player intention to {playerIntention}");
									}
									else throw new Exception($"Could not deselect \"{card3D?.Name}\".");

								}
								else throw new Exception($"Could not deselect \"{selectedCard3D?.Name}\".");
								break;
							}
						case PlayerIntention.UnitMove:
						case PlayerIntention.UnitAttack:
							{
								/*
								* Assumption is that, regardless of current intention, if the player clicks on a card they want to select it to place it.
								*/

								Unit3D selectedUnit3D = selectedObject as Unit3D;
								Card3D cardToSelect = card3D;

								// Deselect current object (should be a unit based on state)
								if (selectedUnit3D != null && selectedUnit3D.OnObjectDeselected())
								{
									OnObjectDeselected?.Invoke(selectedObject);
									// Select this card
									if (cardToSelect != null && cardToSelect.OnObjectSelected())
									{
										OnObjectSelected?.Invoke(cardToSelect);
										// Set intention to 'PlaceCard'
										playerIntention = PlayerIntention.PlaceCard;
										selectedObject = cardToSelect;
										GD.Print($"Selected card {cardToSelect.card.NAME}. Changed player intention to {playerIntention}");
									}
									else throw new Exception($"Could not deselect \"{card3D?.Name}\".");

								}
								else throw new Exception($"Could not deselect \"{selectedUnit3D?.Name}\".");
								break;
							}
					}
					break;
				}
			case Unit3D unit3D:
				{
					switch (playerIntention)
					{
						// TL;DR : Select this unit
						case PlayerIntention.Open:
							{
								/*
								* Assumption is that, since intention is open, the player wants to select this unit to move it.
								Nothing should be selected, so we won't worry about deselecting
								We will select this unit then set player intention to move the unit
								*/

								// Select this unit
								if (unit3D != null && unit3D.OnObjectSelected())
								{
									OnObjectSelected?.Invoke(unit3D);
									// Set intention to 'UnitMove' (if the player wants to attack, they'll have to input for that)
									playerIntention = PlayerIntention.UnitMove;
									selectedObject = unit3D;
									GD.Print($"Selected unit {unit3D.Name}. Changed player intention to {playerIntention}");
								}
								else throw new Exception($"Could not select \"{unit3D?.Name}\".");
								break;
							}
						// TL;DR : Select this unit
						case PlayerIntention.PlaceCard:
							{
								/*
								* Assumption is that, even though intention is to place card the player wants to select this unit to move it instead.
								We will try to deselect the current object (should be a card)
								We will select this unit then set player intention to move the unit
								*/

								Card3D selectedCard3D = selectedObject as Card3D;

								// Deselect current card
								if (selectedCard3D != null && selectedCard3D.OnObjectDeselected())
								{
									OnObjectDeselected?.Invoke(selectedObject);
									// Select this unit
									if (unit3D != null && unit3D.OnObjectSelected())
									{
										OnObjectSelected?.Invoke(unit3D);
										// Set intention to 'UnitMove' (if the player wants to attack, they'll have to input for that)
										playerIntention = PlayerIntention.UnitMove;
										selectedObject = unit3D;
										GD.Print($"Selected unit {unit3D.Name}. Changed player intention to {playerIntention}");
									}
									else throw new Exception($"Could not select \"{unit3D?.Name}\". Either unit is null {unit3D == null} or failed to select");
								}
								else throw new Exception($"If the intention WAS to place a card, then a card should be deselected. Could not deselect \"{selectedObject?.Name}\". No object is selected ({selectedObject == null}) or it failed to deselect.");


								break;
							}
						// TL;DR : Select this unit
						case PlayerIntention.UnitMove:
							{
								/*
								* Assumption is that, even though intention is to move a unit the player wants to select this unit to move it instead.
								We will try to deselect the current object (should be a unit)
								We will select this unit then set player intention to move the unit
								*/

								Unit3D selectedUnit3D = selectedObject as Unit3D;

								// Deselect current object (should be a unit)
								if (selectedUnit3D != null && selectedUnit3D.OnObjectDeselected())
								{
									OnObjectDeselected?.Invoke(selectedObject);
									// Select this unit
									if (unit3D != null && unit3D.OnObjectSelected())
									{
										OnObjectSelected?.Invoke(unit3D);
										// Set intention to 'UnitMove' (if the player wants to attack, they'll have to input for that)
										playerIntention = PlayerIntention.UnitMove;
										selectedObject = unit3D;
										GD.Print($"Selected unit {unit3D.Name}. Changed player intention to {playerIntention}");
									}
									else throw new Exception($"Could not select \"{unit3D?.Name}\".");
								}
								else throw new Exception($"If the intention WAS to move a unit, then a unit should be deselected. Could not deselect \"{selectedObject?.Name}\". No object is selected ({selectedObject == null}) or the call returned false.");

								break;
							}
						// TL;DR : Attack this unit with the currently selected unit
						case PlayerIntention.UnitAttack:
							{
								/*
								* Assumption is that the player wants to attack this unit
								We will try to attack this unit.
								We will deselect the current selected unit if it cannot attack anymore.
								*/
								Unit3D attackUnit3D = selectedObject as Unit3D;
								Unit3D target3D = unit3D;

								if (attackUnit3D != null && target3D != null)
								{
									Axial attackDirection = target3D.unit.pos - attackUnit3D.unit.pos;
									if (main.Instance.gameModel.Unit_TryAttack(attackUnit3D.unit, attackDirection, target3D.unit))
									{
										//If the unit can no longer attack AND cannot move,
										if (!attackUnit3D.unit.CanAttack() && !attackUnit3D.unit.HasMovement(out int dummyInt))
										{
											main.Instance.DS.QueueMessage(false, $"That was a good attack. But that {C_(ORANGE)}{attackUnit3D.unit.card.NAME}{C_()} is all done for this turn.");
											if (attackUnit3D.OnObjectDeselected())
											{
												OnObjectDeselected?.Invoke(selectedObject);
												playerIntention = PlayerIntention.Open;
												OnPlayerAttackMode(false);
												selectedObject = null;
												GD.Print($"Selected object set to null. Changed player intention to {playerIntention}");
											}
											else throw new Exception($"Could not deselect \"{selectedObject?.Name}\".");
										}
										//If the unit can NOT attack anymore, but CAN move,
										else if (attackUnit3D.unit.HasMovement(out dummyInt) && !attackUnit3D.unit.CanAttack())
										{
											GD.Print("Switching intention to move since the unit cannot attack anymore, but can move");
											playerIntention = PlayerIntention.UnitMove;
											OnPlayerAttackMode(false);
										}

										GD.Print($"User has successfully attacked ({target3D.Name})@{target3D.unit.pos} with {attackUnit3D.Name}@{attackUnit3D.unit.pos}. Player intention set to {playerIntention}");
									}
									else
									{
										GD.Print($"Player tried to attack({target3D.Name})@{target3D.unit.pos} with {attackUnit3D.Name}@{attackUnit3D.unit.pos}, but it failed. Given all conditions so far, it should not fail. There is likely a discrepency between the Model and the View");
										GD.PrintErr("Play input error SFX here");
									}


								}
								else throw new Exception($"Player is attempting to attack a unit, but no unit is currently selected({attackUnit3D == null}) OR the target is null({target3D == null}). This should not be possible since intention is set to 'UnitAttack' and we shouldn't reach this case if target is null.");

								break;
							}
					}
					break;
				}
			case Hex3D hex3D:
				{
					switch (playerIntention)
					{
						case PlayerIntention.UnitAttack:
						case PlayerIntention.Open:
							/*
							* Assumption is that the player does not want to do anything to this hex.
							*/
							break;
						case PlayerIntention.PlaceCard:
							{
								/*
								* Assumption is that the player wants to place their currently selected card.
								* We will try to place the selected card.
								* Then try to deselect it if successful.
								*/
								Card3D cardToPlace = selectedObject as Card3D;

								if (hex3D != null && cardToPlace != null)
								{
									if (main.Instance.gameModel.TryPlaceCard_FromHand(0, cardToPlace.card.id, hex3D.AxialPos))
									{
										if (cardToPlace.OnObjectDeselected())
										{
											main.Instance.DS.QueueMessage(false, $"Nice play.");
											OnObjectDeselected?.Invoke(selectedObject);
											playerIntention = PlayerIntention.Open;
											selectedObject = null;
											GD.Print($"User has successfully placed {cardToPlace.card.NAME}@{hex3D.AxialPos}. Player intention set to {playerIntention}");
										}
										else throw new Exception($"Could not deselect \"{selectedObject?.Name}\".");
									}
									else
									{
										main.Instance.DS.QueueMessage(true, $"Sorry, I know your new to this. You can't place your {C_(ORANGE)}{cardToPlace.card.NAME}{C_()} at {hex3D.AxialPos}.");
										GD.Print($"User tried to place a card({cardToPlace.card.NAME})@{hex3D.AxialPos}, but it failed.");
										GD.PrintErr("Play input error SFX here");
									}
								}
								else
									throw new Exception($"User has clicked a hex with the intenion \"{playerIntention}\", but either no card is currently selected({cardToPlace == null}) OR the hex3D is null ({hex3D == null}). Neither should be possible.");
								break;
							}
						case PlayerIntention.UnitMove:
							{
								/*
								* Assumption is that the player wants to move to this open hex with their currently selected unit.
								* We will try to move the selected unit.
								* Then try to deselect it if successful AND if the unit cannot move anymore.
								*/
								Unit3D unitToMove = selectedObject as Unit3D;

								if (hex3D != null && unitToMove != null)
								{
									if (main.Instance.gameModel.Unit_TryMove(true, unitToMove.unit, hex3D.AxialPos))
									{
										//If the unit can NOT move anymore AND cannot attack, then they probably want to deselect this unit
										if (!unitToMove.unit.HasMovement(out int dummyInt) && !unitToMove.unit.CanAttack())
										{
											if (unitToMove.OnObjectDeselected())
											{
												main.Instance.DS.QueueMessage(false, $"Your unit is entering \"do nothing\" mode.");
												OnObjectDeselected?.Invoke(selectedObject);
												playerIntention = PlayerIntention.Open;
												selectedObject = null;
												GD.Print($"Selected object set to null. Changed player intention to {playerIntention}");
											}
											else throw new Exception($"Could not deselect \"{selectedObject?.Name}\".");
										}
										else
										{
											main.Instance.DS.QueueMessage(false, $"You're out of moves with your {C_(ORANGE)}{unitToMove.unit.card.NAME}{C_()}, but you can still attack if you'd like.");
											// In this case, the unit may be out of movement but still have attack left. They probably want to keep the unit selected.
										}

										GD.Print($"User has successfully moved unit {unitToMove.Name} to {unitToMove.unit.pos}. Player intention set to {playerIntention}");
									}
									else
									{
										GD.Print($"User tried to move a unit({unitToMove.Name}) to {hex3D.AxialPos}, but it failed.");
										GD.PrintErr("Play input error SFX here");
									}

								}
								else
									throw new Exception($"User has clicked a hex with the intenion \"{playerIntention}\", but either no unit is currently selected({unitToMove == null}) OR the hex3D is null ({hex3D == null}). Neither should be possible.");

								break;
							}
					}
					break;
				}
			default:
				throw new Exception($"Uncaught case for type {node3D.GetType()}");
		}

		return false;
	}

	public bool HandleObjectClickOff(Node3D node3D)
	{
		if (playerIntention == PlayerIntention.DISABLED)
			return false;

		return false;
	}

	int onCardSelectMsgIndex = 0;
	Random random = new Random();

	private void DisplayMessageOnCardSelect(Card3D card3D)
	{
		if (main.Instance.gameModel.RoundCounter == 0 && card3D.card.NAME == Model.Card.BUSSER_RACOON_NAME)
		{
			main.Instance.DS.QueueMessage(false, $"That unit will make more sense next round. It can steal resources, but, to keep things simple, we don't have those in our decks right right now.");
			return;
		}
		
		if (main.Instance.gameModel.RoundCounter == 0 && card3D.card.NAME == Model.Card.BIG_MOE_NAME)
		{
			main.Instance.DS.QueueMessage(false, $"That's me. I'm a hungry man, but I hit hard and knock back units. If they can't move, they take more damage. Anything it collides with takes damage too.");
			return;
		}
		
		if (main.Instance.gameModel.RoundCounter == 0 && card3D.card.NAME == Model.Card.EXPO_PIGEON_NAME)
		{
			main.Instance.DS.QueueMessage(false, $"Fastest expo I've ever seen. He can move fast AND move over units. But he's weak.");
			return;
		}
		
		if (main.Instance.gameModel.RoundCounter == 0 && card3D.card.NAME == Model.Card.EXPO_PIGEON_NAME)
		{
			main.Instance.DS.QueueMessage(false, $"Fastest expo I've ever seen. He can move fast AND move over units. But he's weak.");
			return;
		}


		int ran = random.Next(0, 2);
		if (ran != 0)
			return;

		string[] onCardSelectMsgs = new string[]
		{
		$"Ah, looking to play {card3D.card.NAME}? I believe that's the Sicilian Pizza defense... a classic.",
		$"Ah, looking to play {card3D.card.NAME}? I think that's a wise decision. Maybe you'd like to apply as a line cook after this? Seriously. We're really understaffed.",
		$"Ah, looking to play {card3D.card.NAME}? That's not the mode I would've taken, but I can respect it.",
		$"Ah, looking to play {card3D.card.NAME}? That reminds me of the time I dropped my phone in the fryer back when I was a young, dumb line boy. Without a second thought I reached in to grab it. My hand went fried mode real quick.",
		$"Ah, looking to play {card3D.card.NAME}? You ever watch Bob's Burgers? Or Spongebob Squarepants? My burgers beat both their burgers.",
		$"Ah, looking to play-- did you hear that? No, no, no, I can't believe it... newbie's making a code 13 on the lettuce! I'll be right back...",
		$"Ah, looking to play {card3D.card.NAME}? That reminds me of the mode of '87.",
		$"Ah, looking to play... have I-- have I ever told you about the mode of-- Nevermind",
		$"Ah, looking to play {card3D.card.NAME}? I totally would play that. That's a great move.",
		$"Ah, looking to play... look I'm running out of the mental bandwidth to keep coming up with new things to say. I'm just gonna start repeating things and hope you don't notice."
		};


		main.Instance.DS.QueueMessage(false, onCardSelectMsgs[onCardSelectMsgIndex % onCardSelectMsgs.Length]);
		onCardSelectMsgIndex++;
	}
}
