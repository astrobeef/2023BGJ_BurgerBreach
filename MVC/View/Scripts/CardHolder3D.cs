using Godot;
using Model;
using System;
using System.Linq;

public partial class CardHolder3D : Node3D
{
	private Model_Game _gameModel;

	Card3D[] Cards3D;
	Node3D[] CardSlots3D;

	private static string CARD_BURGER = "res://MVC/View/3D Assets/Prototype/Card/card_burger.tscn";
	private static string CARD_MOE = "res://MVC/View/3D Assets/Prototype/Card/card_moe.tscn";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AwaitReady();
		CardSlots3D = GetCardSlotChildren();
	}

	private Node3D[] GetCardSlotChildren()
	{
		Node3D[] cardSlots = new Node3D[GetChildCount()];

		Godot.Collections.Array<Node> children = GetChildren(false);
		for (int i = 0; i < children.Count; i++)
		{
			Node3D child = (Node3D)children[i];
			cardSlots[i] = child;
		}

		return cardSlots;
	}

	private async void AwaitReady()
	{
		while (main.Instance?.gameModel == null)
		{
			await System.Threading.Tasks.Task.Delay(5);
		}

		_gameModel = main.Instance.gameModel;

		_gameModel.OnCardDrawn += OnCardDrawn;
		_gameModel.OnCardRemoved += OnCardRemoved;
	}

	private void OnCardDrawn(int playerIndex, Card cardDrawn, Card[] hand, int drawnCount)
	{
		if (playerIndex == 0)
		{
			main.Instance.SoundController?.Play(sound_controller.SFX_CARD_DRAW_NAME);

			PackedScene scene;

			switch (cardDrawn.NAME)
			{
				case (Card.BURGER_NAME):
					scene = GD.Load<PackedScene>(CARD_BURGER);
					break;
				case (Card.BIG_MOE_NAME):
					scene = GD.Load<PackedScene>(CARD_MOE);
					break;
				default:
					GD.PrintErr($"Uncaught case for card name \"{cardDrawn.NAME}\"");
					return;
			}

			foreach (Node3D cardSlot in CardSlots3D)
			{
				if (cardSlot.GetChildCount() == 0)
				{
					CreateCard3D(scene, cardSlot, cardDrawn);
					break;
				}
			}

			if (!IsHandModelMatchingVisual(hand))
				GD.PrintErr("Handle mismatch");
		}
	}

	private void OnCardRemoved(int playerIndex, Card cardRemoved, Card[] hand)
	{
		if (playerIndex == 0)
		{
			Card3D cardRemoved3D = null;

			foreach (Node3D cardSlot in CardSlots3D)
			{
				if (SlotHasCard(cardSlot, out Card3D card3D))
				{
					if (card3D.card == cardRemoved)
					{
						cardSlot.RemoveChild(card3D);
						cardRemoved3D = card3D;
						break;
					}
				}
			}

			if (!IsHandModelMatchingVisual(hand))
				GD.PrintErr("Handle mismatch");

			if (cardRemoved3D == null)
				GD.PrintErr($"Could not remove card {cardRemoved} from visual");
			else
				cardRemoved3D.QueueFree();
		}
	}

	private bool SlotHasCard(Node3D slot, out Card3D card3D)
	{
		card3D = null;

		if (slot.GetChildCount() == 1)
		{
			card3D = slot.GetChild<Card3D>(0);
			if (card3D != null)
			{
				return true;
			}
			else
			{
				GD.PrintErr("card slot child is not of type Card3D");
				return false;
			}
		}
		else if (slot.GetChildCount() > 1)
		{
			GD.PrintErr("Card slot has more than 1 child, this should not be possible");
			return false;
		}
		else
		{
			return false;
		}
	}

	private void CreateCard3D(PackedScene scene, Node3D parentSlot, Card cardModel)
	{
		Card3D card3D = (Card3D)scene.Instantiate();
		card3D.card = cardModel;
		parentSlot.AddChild(card3D);
	}

	private bool IsHandModelMatchingVisual(Card[] handModel)
	{
		// Start at true and disprove throughout
		bool[] mismatches = Enumerable.Repeat(true, handModel.Length).ToArray();

		foreach (Node3D cardSlot in CardSlots3D)
		{
			if (SlotHasCard(cardSlot, out Card3D card3D))
			{
				int i;
				for (i = 0; i < handModel.Length; i++)
				{
					Card cardModel = handModel[i];

					if (card3D.card == cardModel)
					{
						mismatches[i] = false;
						break;
					}
				}
				if (mismatches[i])
				{
					GD.PrintErr($"card slot {card3D.Name} with card {card3D.card} does not have a matching card in the hand model");
					return false;
				}
			}
		}

		//Ensure no mismatches (its possible some cards were missed)
		foreach (bool mismatch in mismatches)
		{
			if (mismatch)
			{
				return false;
			}
		}
		return true;
	}
}
