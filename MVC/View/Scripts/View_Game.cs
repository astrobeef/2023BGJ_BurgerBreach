using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AxialCS;
using EditorTools;
using Godot;
using Model;

namespace View
{
    public partial class View_Game : Control
    {
        public bool isInit = false;
        public bool isInitializing = false;
        public object initLock = new object();

        EDITOR_Tool editor;
        Model_Game model;

        Node _LogMessagesContainer;
        Label[] _Logs;
        int iterateLog = 0, _logCount = 0;

        Node _CardHolder;
        List<Button> _CardBtns;
        int _cardCount = 0;

        #region  INITIALIZATION

        public override void _Ready()
        {
            if (!isInit)
            {
                isInitializing = true;

                if (!InitLogs()) GD.PrintErr("Could not initialize logs");
                else Print("Success! Logs initialized!");

                if (!InitHeldCards()) GD.PrintErr("Could not initialize held cards");
                else Print("Success! Held cards initialized!");

                if (!InitPlayerInput()) GD.PrintErr("Could not initialize player button");
                else Print("Success! Player button initialized!");

                editor = FindEditor();
                AwaitInitialization();

                DrawGrid();
            }
        }

        private bool InitLogs()
        {
            Node LogMessages = FindChild("LogMessages");

            if (LogMessages == null)
            {
                GD.PrintErr("Could not find log messages");
                return false;
            }

            _Logs = LogMessages.GetChildren().OfType<Label>().ToArray();

            if (_Logs == null || _Logs.Length == 0)
            {
                GD.PrintErr("Could not find log labels");
                return false;
            }

            _logCount = _Logs.Length;
            return _logCount > 0;
        }

        private bool InitHeldCards()
        {
            Node CardHolder = FindChild("CardHolder");

            if (CardHolder == null)
            {
                GD.PrintErr("Could not find CardHolder");
                return false;
            }

            _CardBtns = new List<Button>();
            _CardBtns.AddRange(CardHolder.GetChildren().OfType<Button>().ToArray());

            if (_CardBtns == null || _CardBtns.Count == 0)
            {
                GD.PrintErr("Could not find cards");
                return false;
            }

            _cardCount = _CardBtns.Count;
            return _cardCount > 0;
        }

        private bool InitPlayerInput()
        {
            playerInput_1 = FindChild("InputButton") as Button;

            if (playerInput_1 == null)
            {
                GD.PrintErr("Could not find InputButton");
                return false;
            }
            else
            {
                return true;
            }
        }

        private EDITOR_Tool FindEditor()
        {
            EDITOR_Tool editor = GetParent() as EDITOR_Tool;

            if (editor == null)
            {
                GD.PrintErr("Could not find Editor as parent. Check heirarchy.");
            }
            else
            {
                editor.OnProcess += OnProcess;
            }

            return editor;
        }

        private async void AwaitInitialization()
        {
            isInit = await HandleModelAssignment();
            isInitializing = false;

            Print("View is ready!");
        }

        private async Task<bool> HandleModelAssignment()
        {
            bool isModelInit = await AssignModel(0);

            if (isModelInit)
            {
                InitEvents();
            }

            return isModelInit;
        }

        private async Task<bool> AssignModel(int failsafe)
        {
            failsafe++;
            if (main.Instance?.gameModel == null)
            {
                if (failsafe < 1000)
                {
                    await Task.Delay(1);
                    return await AssignModel(failsafe);
                }
                else
                {
                    GD.PrintErr("Could not assign model reference.");
                    return false;
                }
            }
            else
            {
                model = main.Instance.gameModel;
                return true;
            }
        }

        #endregion

        private void OnProcess()
        {
        }

        #region Model Driven Events

        private void InitEvents()
        {
            GD.Print("Init View events");

            model.OnGameStart += OnGameStart;
            model.OnRoundStart += OnRoundStart;
            model.OnTurnStart += OnTurnStart;
            model.OnTurnEnd += OnTurnEnd;

            model.OnDeckBuildStart += OnDeckBuildStart;
            model.OnDeckBuildAddedCard += OnDeckBuildAddedCard;
            model.OnDeckBuildFinished += OnDeckBuildFinished;

            model.OnUnitAddedToBoard += OnUnitAddedToBoard;
            model.OnUnitMove += OnUnitMove;
            model.OnUnitAttack += OnUnitAttack;
            model.OnBaseDestroyed += OnBaseDestroyed;
            model.OnDamaged += OnDamaged;
            model.OnUnitDeath += OnDeath;
            model.OnCollision += OnCollision;

            model.OnCardDrawn += OnCardDrawn;
            model.OnCardDrawn_fail += OnCardDrawn_fail;
            model.OnCardRemoved += OnCardRemoved;

            // INPUT
            model.OnAwaitStartGame += OnAwaitStartGame;
            model.OnAwaitDrawCard += OnAwaitDrawCard;

            model.OnAwaitTurnActions += OnAwaitTurnActions;
        }

        private void OnGameStart(Card[] CardSet, Card[] CardSet_NoBases)
        {
            Print("Game started!");
            DisplayCardSet(CardSet, CardSet_NoBases);
        }

        private void OnRoundStart(int roundCounter)
        {
            Print("-------------------------");
            Print($"----- START ROUND {roundCounter} -----");
            Print("-------------------------");
        }

        private void OnTurnStart(int turnCounter, int turnPlayerIndex)
        {
            GD.Print("------------------------");
            GD.Print($"----- START TURN {turnCounter + 1} -----");
            GD.Print($"It is player [{turnPlayerIndex}]'s turn");
            GD.Print("------------------------");
        }

        private void OnTurnEnd(int turnCounter, int playerIndex)
        {
            if(model.OnCardRemoved != null)
                model.OnCardRemoved -= SubscribeAllCards;
        }

        private void DisplayCardSet(Card[] CardSet, Card[] CardSet_NoBases)
        {
            Print("----- CARD SET W/ BASES -----");

            for (int i = 0; i < CardSet.Length; i++)
            {
                Card card = CardSet[i];
                Print($"[{i}] = {card}");
            }

            Print("----- CARD SET W/O BASES -----");

            for (int i = 0; i < CardSet_NoBases.Length; i++)
            {
                Card card = CardSet_NoBases[i];
                Print($"[{i}] = {card}");
            }
        }

        private void OnDeckBuildStart(int ownerIndex)
        {
            Print($"----- Initializing deck[{ownerIndex}] -----");
        }

        private void OnDeckBuildAddedCard(int ownerIndex, int cardIndexInDeck, Card card)
        {
            Print($"Deck [{ownerIndex}][{cardIndexInDeck}] : {card}");
        }

        private void OnDeckBuildFinished(int ownerIndex)
        {
            Print($"----- Initialized deck[{ownerIndex}] -----");
            Print("-------------------------------");
        }

        private void OnUnitAddedToBoard(Unit newUnit)
        {
            Print($"Player {newUnit.ownerIndex} placing card {newUnit.name} at location {newUnit.pos}");

            Vector2 pxPos = Axial.AxToPx(_boardOffset, _sideLength, newUnit.pos);
            HexagonDraw hexagonDraw = new HexagonDraw(pxPos, _sideLength, GetColor(newUnit));

            // Remove currently rendered hex if needed
            if (hexRenderer.IsHexRendered(newUnit.pos))
                hexRenderer.RemoveHex(newUnit.pos);

            // Render hex
            hexRenderer.AddHex(newUnit.pos, hexagonDraw);
        }

        private void OnUnitMove(Axial oldPos, Unit unit)
        {
            Print($"Player {unit.ownerIndex} moved unit {unit.name} from {oldPos} to {unit.pos}. Calculated displacement: {unit.pos - oldPos}.");

            Vector2 pxPos = Axial.AxToPx(_boardOffset, _sideLength, unit.pos);
            HexagonDraw hexagonDraw = new HexagonDraw(pxPos, _sideLength, GetColor(unit));

            // Remove old rendered hex if needed
            if (hexRenderer.IsHexRendered(oldPos))
            {
                HexagonDraw draw = hexRenderer.HexAxialDrawDictionary[oldPos];
                HexagonDraw redrawGrid = new HexagonDraw(draw.origin, draw.side_length, gridColors[0]);
                hexRenderer.RemoveHex(oldPos);
                hexRenderer.AddHex(oldPos, redrawGrid);
            }

            // Remove currently rendered hex if needed
            if (hexRenderer.IsHexRendered(unit.pos))
                hexRenderer.RemoveHex(unit.pos);

            // Render hex
            hexRenderer.AddHex(unit.pos, hexagonDraw);
        }

        private void OnUnitAttack(Unit attacker, Unit target)
        {
            Print($"Unit ${attacker.name} attacked {target.name}!");

            Color attackerColor = GetColor(attacker);
        }

        private void OnDamaged(Unit unit)
        {
            Print($"Unit ${unit.name} was damaged!");

            IProgress<(Axial, HexagonDraw)> progress_add = new Progress<(Axial, HexagonDraw)>(tuple =>
            {
                Axial ax = tuple.Item1;
                HexagonDraw hex = tuple.Item2;
                hexRenderer.AddHex(ax, hex);
            });

            IProgress<Axial> progress_remove = new Progress<Axial>(axial =>
            {
                hexRenderer.RemoveHex(axial);
            });

            Task.Run(() =>
            {
            });
        }

        private void OnBaseDestroyed(Unit Base)
        {
            Print($"GAME OVER!");
        }

        private void OnCollision(Unit mover, Unit occupant)
        {
            Print($"Unit ${mover.name} collided with {occupant.name}!");
        }

        private void OnDeath(Unit unit)
        {
            Print($"Unit ${unit.name} destroyed!");
            // Remove currently rendered hex if needed
            if (hexRenderer.IsHexRendered(unit.pos))
            {
                HexagonDraw draw = hexRenderer.HexAxialDrawDictionary[unit.pos];
                HexagonDraw redrawGrid = new HexagonDraw(draw.origin, draw.side_length, gridColors[4]);
                hexRenderer.RemoveHex(unit.pos);
                hexRenderer.AddHex(unit.pos, redrawGrid);
            }
        }

        private void OnCardDrawn(int ownerIndex, Card card, Card[] heldCards, int cardsDrawn)
        {
            Print($"Player {ownerIndex} drew a card ({card.NAME}), increasing their hand to {heldCards.Length}. Their drawn count has incremented to {cardsDrawn}");

            if (ownerIndex == 0)
            {
                // Increment cards until it reaches the same length as held cards
                if (_CardBtns.Count < heldCards.Length)
                {
                    while (_CardBtns.Count < heldCards.Length)
                    {
                        GD.PrintErr("This is not working");
                        Button dup = _CardBtns[0].Duplicate() as Button;
                        _CardHolder.AddChild(dup);
                        _CardBtns.Add(dup);
                    }
                }

                for (int i = 0; i < heldCards.Length; i++)
                {
                    Card iCard = heldCards[i];

                    _CardBtns[i].Text = iCard.NAME;
                }
            }
        }

        private void OnCardRemoved(int ownerIndex, Card cardRemoved, Card[] heldCards)
        {
            if (ownerIndex == 0)
            {
                // Increment cards until it reaches the same length as held cards
                if (_CardBtns.Count < heldCards.Length)
                {
                    while (_CardBtns.Count < heldCards.Length)
                    {
                        GD.PrintErr("This is not working");
                        Button dup = _CardBtns[0].Duplicate() as Button;
                        _CardHolder.AddChild(dup);
                        _CardBtns.Add(dup);
                    }
                }

                foreach (Button card in _CardBtns)
                {
                    card.Text = "";
                }

                for (int i = 0; i < heldCards.Length; i++)
                {
                    Card card = heldCards[i];

                    _CardBtns[i].Text = card.NAME;
                }
            }
        }

        private void OnCardDrawn_fail(int ownerIndex, int cardsDrawn, int deckCount)
        {
            Print($"Player {ownerIndex} cannot draw any more cards because their drawn count ({cardsDrawn}) is equal to their deck count ({deckCount})");
        }

        private void Print(string message)
        {
            if (_logCount > 0)
            {
                // Shift all log entries down by one
                for (int i = 0; i < _logCount - 1; i++)
                {
                    _Logs[i].Text = _Logs[i + 1].Text;
                }

                // Set the last log entry to the new message
                int lastIndex = _logCount - 1;
                _Logs[lastIndex].Text = message;

                iterateLog++;
            }
            else if (!tryReadyAgain)
            {
                tryReadyAgain = true;
                _Ready();
                Print(message);
            }
            else
            {
                GD.PrintErr("Failed to initialize logs");
            }

            GD.Print(message);
        }

        #endregion

        #region View Driven Actions

        public Button playerInput_1;

        private void OnAwaitStartGame()
        {
            playerInput_1.Text = "Start Game";
            playerInput_1.Pressed += HandleInput_StartGame;
        }

        private void HandleInput_StartGame()
        {
            playerInput_1.Text = "No input Registered";
            model.triggerStartGame = true;
            playerInput_1.Pressed -= HandleInput_StartGame;
        }

        private void OnAwaitDrawCard(int playerIndex, int drawIndex)
        {
            if(playerIndex != 0)
                return;

            playerInput_1.Text = "Draw Card";
            playerInput_1.Pressed += HandleInput_DrawCard;
        }

        private void HandleInput_DrawCard()
        {
            playerInput_1.Text = "No Input Registered";
            model.TriggerDrawCard = true;
            playerInput_1.Pressed -= HandleInput_DrawCard;
        }

        private void OnAwaitTurnActions(int playerIndex, int turnIndex)
        {
            if (playerIndex == 0)
            {
                AllowPlayHandCards(turnIndex);
                AllowMoveUnits(turnIndex);
            }
        }

        private void AllowPlayHandCards(int turnIndex)
        {
            SubscribeAllCards(0, Card.EMPTY, null);

            //When a card is removed from the hand, resubscribe to update for shifted indexes
            model.OnCardRemoved += SubscribeAllCards;
        }

        private void AllowMoveUnits(int turnIndex)
        {
            main.Instance.OnProcess += OnMouseInput_MoveUnitRequest;
        }

        private bool _leftMouseClicked = false;
        private Axial _unitToMove = Axial.Empty;

        private void OnMouseInput_MoveUnitRequest()
        {
            Utility.MouseMoveObserver MouseMoveObserver = main.Instance.MouseMoveObserver;

            if (_leftMouseClicked)
                _leftMouseClicked = Input.IsMouseButtonPressed(MouseButton.Left);

            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                if (!_leftMouseClicked)
                {
                    _leftMouseClicked = true;
                    MouseMoveObserver._pos_cur = GetViewport().GetMousePosition();

                    Axial axMouse = Axial.PxToAx(_boardOffset, _sideLength, MouseMoveObserver._pos_cur);

                    GD.Print($"Registered mouse input. Mouse position: (px:{MouseMoveObserver._pos_cur}, ax:{axMouse}");


                    // If the user has selected a unit to move
                    if (_unitToMove == Axial.Empty)
                    {
                        if (model.Board.IsAxialOnGrid(axMouse))
                        {
                            if (model.ActiveBoard_IsAxialOccupied(axMouse, out int boardIndex))
                            {
                                GD.Print($"User has clicked on a unit on the board");

                                _unitToMove = axMouse;
                            }
                        }
                    }
                    else
                    {
                        if (model.Board.IsAxialOnGrid(axMouse))
                        {
                            if (model.IsPlacementLocationValid(axMouse))
                            {
                                GD.Print($"User can move the unit to this open tile");
                                if (model.Unit_TryMove(true, 0, _unitToMove, axMouse))
                                    _unitToMove = Axial.Empty;
                            }
                            else
                                _unitToMove = Axial.Empty;
                        }
                        else
                            _unitToMove = Axial.Empty;
                    }
                }
            }
        }

        private void SubscribeAllCards(int dummyInt, Card cardRemoved, Card[] dummyHand)
        {
            for (int i = 0; i < _CardBtns.Count; i++)
            {
                Button cardBtn = _CardBtns[i];
                SubscribeCardPressed(cardBtn, i);
            }
        }

        private void SubscribeCardPressed(Button card, int index)
        {
            card.Pressed -= OnCardPressed;

            card.Pressed += OnCardPressed;

            void OnCardPressed()
            {
                HandleInput_PlaceCard(index);
                card.Pressed -= OnCardPressed;
            }
        }

        private void HandleInput_PlaceCard(int cardIndex)
        {
            model.TryPlaceCardRandomly(0, cardIndex);
        }

        #endregion

        bool tryReadyAgain = false;

        #region RENDERER
        HexagonRenderer hexRenderer;

        private Godot.Color[] gridColors = new Color[] {
    new Color(0.40f, 0.40f, 0.40f),
    new Color(0.42f, 0.42f, 0.42f),
    new Color(0.44f, 0.44f, 0.44f),
    new Color(0.46f, 0.46f, 0.46f),
    new Color(0.48f, 0.48f, 0.48f),
    new Color(0.50f, 0.50f, 0.5f)};

        private Color[] BaseColors = new Color[] {
    new Color(0.5f, 0.8f, 0.5f), // Desaturated Green
    new Godot.Color(0.8f, 0.5f, 0.5f) // Desaturated Red
    };

        private Color[] ResourceColors = new Color[] {
    new Godot.Color(0.7f, 0.6f, 0.8f), // Desaturated Light Purple
    new Godot.Color(0.6f, 0.5f, 0.7f)  // Desaturated Dark Purple
    };

        private Color[] OffenseColors = new Color[] {
    new Godot.Color(0.8f, 0.7f, 0.5f), // Desaturated Light Orange
    new Godot.Color(0.8f, 0.6f, 0.4f) // Desaturated Dark Orange
    };

        private Color GetColor(Unit unit)
        {
            if (unit.ownerIndex > BaseColors.Length)
            {
                GD.PrintErr($"Not enough colors for the number of players : {unit.ownerIndex}");
                return gridColors[0];
            }

            switch (unit.type)
            {
                case Card.CardType.Base:
                    return BaseColors[unit.ownerIndex];
                case Card.CardType.Resource:
                    return ResourceColors[unit.ownerIndex];
                case Card.CardType.Offense:
                    return OffenseColors[unit.ownerIndex];
                default:
                    GD.PrintErr($"Failed to catch case {unit.type}");
                    return gridColors[0];
            }
        }


        Vector2 _boardOffset = new Vector2(300, 500);
        float _sideLength = 20.0f;

        private void DrawGrid()
        {
            hexRenderer = new HexagonRenderer();
            main.GetActiveScene(this, this.Name).AddChild(hexRenderer);

            Axial ax_origin = new Axial(0, 0);
            GD.Print($"These axials do not have a screen space position. The first axial is located in Axial position: {ax_origin}");

            IProgress<(Axial, HexagonDraw)> progress = new Progress<(Axial, HexagonDraw)>(tuple =>
            {
                Axial ax = tuple.Item1;
                HexagonDraw hex = tuple.Item2;
                hexRenderer.AddHex(ax, hex);
            });

            Task.Run(() =>
            {
                while (model == null || model.Board.Axials == null || model.Board.Axials.Length == 0)
                {
                    System.Threading.Thread.Sleep(100);
                }

                int colorIndex = 0;
                foreach (Axial ax in model.Board.Axials)
                {
                    colorIndex++;
                    System.Threading.Thread.Sleep(1);
                    Vector2 pos = Axial.AxToPx(_boardOffset, _sideLength, ax);
                    HexagonDraw hexagonDraw = new HexagonDraw(pos, _sideLength, gridColors[colorIndex % gridColors.Length]);
                    GD.Print($"Adding {ax} @ {pos}");
                    progress.Report((ax, hexagonDraw));
                }
            });
        }

        #endregion

    }
}