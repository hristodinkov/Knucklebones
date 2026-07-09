using NetworkConnections;
using OSCTools;
using System;

namespace TcpEchoServerPolling
{
    class GameRoom
    {
        #region Fields

        public TcpNetworkConnection player1;
        public TcpNetworkConnection player2;

        public Model p1Model;
        public Model p2Model;
        public bool turnOrder;
        public int dice1, dice2;
        public int selectedDiceP1 = -1;
        public int selectedDiceP2 = -1;
        public bool p1WantsRematch = false;
        public bool p2WantsRematch = false;
        public bool gameStarted = false;
        private bool isGameOver = false;
        private bool forceEmpty = false;

        public string p1Token;
        public string p2Token;
        public bool p1Disconnected = false;
        public bool p2Disconnected = false;
        public DateTime p1DisconnectTime;
        public DateTime p2DisconnectTime;
        private int lastSentCountdown = -1;

        private const float ReconnectWindowSeconds = 30f;
        private const float MoveTimeoutSeconds = 60f;
        private float moveTimeRemaining = MoveTimeoutSeconds;
        private bool moveTimerPaused = false;
        private DateTime lastTickTime;
        private int lastSentMoveCountdown = -1;

        #endregion

        #region Setup and Reset
        public GameRoom()
        {
            ResetGame();
        }

        public void ResetGame()
        {
            p1Model = new Model(1);
            p2Model = new Model(2);
            selectedDiceP1 = -1;
            selectedDiceP2 = -1;
            turnOrder = true;
            isGameOver = false;
            gameStarted = false;
            forceEmpty = false;
            p1WantsRematch = false;
            p2WantsRematch = false;
            moveTimeRemaining = MoveTimeoutSeconds;
            lastTickTime = DateTime.Now;
            lastSentMoveCountdown = -1;

        }
        public void ResetConnectionState()
        {
            Console.WriteLine("Reset connection has been called");
            p1Disconnected = false;
            p2Disconnected = false;
            p1Token = null;
            p2Token = null;
        }
        #endregion

        #region Player Management 

        public bool IsFull() {
            return player1 != null && player2 != null;
        }

        public bool Contains(TcpNetworkConnection connection)
        {
            return player1 == connection || player2 == connection;
        }

        public int GetPlayerIndex(TcpNetworkConnection connection)
        {
            if (connection == player1) return 0;
            if (connection == player2) return 1;
            return -1;
        }

        private TcpNetworkConnection GetOpponent(int playerIndex)
        {
            if(playerIndex == 0)
            {
                return player2;
            }
            else
            {
                return player1;
            }
        }

        public bool IsEmpty()
        {
            if (forceEmpty) return true;

            bool p1Gone = player1 == null || player1.Status == ConnectionStatus.Disconnected;
            bool p2Gone = player2 == null || player2.Status == ConnectionStatus.Disconnected;

            if (!p1Gone || !p2Gone) return false;

            if (p1Disconnected && p2Disconnected)
            {
                ResetConnectionState();
                return true;
            }
            bool p1Expired = !p1Disconnected || (DateTime.Now - p1DisconnectTime).TotalSeconds >= ReconnectWindowSeconds;
            bool p2Expired = !p2Disconnected || (DateTime.Now - p2DisconnectTime).TotalSeconds >= ReconnectWindowSeconds;

            if (p1Expired && p2Expired)
            {
                ResetConnectionState();
                return true;
            }

            return false;
        }

        public void AddPlayer(TcpNetworkConnection connection, string token)
        {
            if (player1 == null)
            {
                player1 = connection;
                p1Token = token;
                Console.WriteLine($"Assigned {connection.Remote} as player 1 in room");
            }
            else if (player2 == null)
            {
                player2 = connection;
                p2Token = token;
                Console.WriteLine($"Assigned {connection.Remote} as player 2 in room");
            }

            SendPlayerInfo(connection);

            if (IsFull())
            {  
                ResetGame();
                gameStarted = true;
                Console.WriteLine("Room full,starting game...");
                BroadcastStartGame();
                turnOrder = true;
                BroadcastTurnChange(0);
                RollDice();
            }
        }

        public void ReconnectPlayer(TcpNetworkConnection connection, int index)
        {
            lastSentCountdown = -1;
            if (index == 0)
            {
                player1 = connection;
                p1Disconnected = false;
            }
            else
            {
                player2 = connection;
                p2Disconnected = false;
            }
            Console.WriteLine($"Player {index} reconnected.");

            SendPlayerInfo(connection);

            OSCMessageOut msg = new OSCMessageOut("/OpponentReconnected");

            TcpNetworkConnection opponent = GetOpponent(index);
            if (opponent != null)
            {
                opponent.Send(msg.GetBytes());
            }

            moveTimerPaused = false;
            lastTickTime = DateTime.Now;

            SendFullState(connection);
        }

        #endregion

        #region Tick and timers

        public void Tick()
        {
            if (isGameOver && forceEmpty)
            {
                return;
            }
            if (p1Disconnected && !p2Disconnected)
            {
                ReconnectionTimeout(0, p1DisconnectTime, player2);
            }
            else if (p2Disconnected && !p1Disconnected)
            {
                ReconnectionTimeout(1, p2DisconnectTime, player1);
            }
            else if (!p1Disconnected && !p2Disconnected && gameStarted && IsFull() && !isGameOver)
            {
                TickMoveTimer();
            }

        }
        private void TickMoveTimer()
        {
            if (moveTimerPaused) return;
            moveTimeRemaining -= (float)(DateTime.Now - lastTickTime).TotalSeconds;
            lastTickTime = DateTime.Now;
            int remainingInt = Math.Max(0, (int)Math.Ceiling(moveTimeRemaining));
            
            if (remainingInt != lastSentMoveCountdown)
            {
                lastSentMoveCountdown = remainingInt;
                //Console.WriteLine($"Sending move countdown: {remainingInt}");
                OSCMessageOut countdown = new OSCMessageOut("/MoveCountdown").AddInt(remainingInt);
                Broadcast(countdown.GetBytes());
            }
            if (moveTimeRemaining <= 0)
            {
                lastSentMoveCountdown = -1;
                int loser = turnOrder ? 0 : 1;
                int winner = turnOrder ? 1 : 0;
                //Console.WriteLine($"Player {loser} ran out of time. Player {winner} wins.");
                GameOverRpc(winner);
            }
        }

        private void ReconnectionTimeout(int playerIndex, DateTime disconnectTime,TcpNetworkConnection opponent)
        {
            float elapsed = (float)(DateTime.Now - disconnectTime).TotalSeconds;
            float remaining = ReconnectWindowSeconds - elapsed;
            int remainingInt = Math.Max(0, (int)Math.Ceiling(remaining));

            if (remainingInt != lastSentCountdown) 
            {
                lastSentCountdown = remainingInt;
                //Console.WriteLine($"Sending countdown: {remainingInt}");
                OSCMessageOut countdown = new OSCMessageOut("/ReconnectCountdown").AddInt(remainingInt);
                if(opponent != null)
                {
                    opponent.Send(countdown.GetBytes());
                }
                
            }

            if (remaining <= 0)
            {
                lastSentCountdown = -1;
                if (playerIndex == 0)
                {
                    GameOverRpc(1);
                    p1Token = null;
                    p1Disconnected = false;

                }
                else
                {
                    GameOverRpc(0);
                    p2Token = null;
                    p2Disconnected = false;
                }
            }
        }

        #endregion

        private bool CheckGameOver(int[,] grid)
        {
            foreach (var cell in grid)
            {
                if (cell == 0)
                {
                    return false;
                }
            }
            return true;
        }


        //----Incomign message handling methods----

        #region Incomming message handlers

        // This message is handled both in the SelectDice & SelectColumn state! (Can change mind and select the other die)

        // State is determined by bool turnOrder + whether selectedDiceP1/P2 is not -1
        public void HandleChooseDice(int playerIndex, int diceIndex)
        {
            if (playerIndex < 0 || playerIndex > 1) return;

            int chosenDice;
            if (diceIndex == 0)
            {
                chosenDice = dice1;
            }
            else
            {
                chosenDice = dice2;
            }
            if (chosenDice < 1 || chosenDice > 6)
            {
                Console.WriteLine($"Invalid dice value {chosenDice}. Ignoring value.");
                return;
            }
            if (playerIndex == 0)
            {
                selectedDiceP1 = chosenDice;   
            }
            else if (playerIndex == 1)
            {
                selectedDiceP2 = chosenDice;
            }
            Console.WriteLine($"Room: Player {playerIndex} selected dice index {diceIndex} with value {chosenDice}");
        }

        public void HandleChooseColumn(int playerIndex, int col)
        {
            if (playerIndex < 0 || playerIndex > 1) return;
            if (col < 0 || col > 2) return;
            if (isGameOver) return;

            int expectedPlayer;
            if (turnOrder)
            {
                expectedPlayer = 0;
            }
            else
            {
                expectedPlayer = 1;
            }

            if (playerIndex != expectedPlayer)
            {
                Console.WriteLine($"Room: Player {playerIndex} tried to move out of turn.");
                return;
            }

            int selected;
            Model add, remove;

            if (playerIndex == 0)
            {
                selected = selectedDiceP1;
                add = p1Model;
                remove = p2Model;
            }
            else
            {
                selected = selectedDiceP2;
                add = p2Model;
                remove = p1Model;
            }

            if (selected == -1)
            {
                Console.WriteLine($"Room: Player {playerIndex} has not selected a dice.");
                return;
            }

            if (!add.TryAddNewDice(selected, col)) return;

            remove.TryRemoveNumber(col, selected);

            selectedDiceP1 = -1;
            selectedDiceP2 = -1;

            BroadcastGridUpdate(playerIndex, col);
            int opponent = (playerIndex == 0) ? 1 : 0;
            BroadcastGridUpdate(opponent, col);
            BroadcastScoreUpdate();

            if (CheckGameOver(add.grid))
            {
                int scoreP1 = p1Model.CalculateGridScore();
                int scoreP2 = p2Model.CalculateGridScore();
                int winner;
                if (scoreP1 > scoreP2)
                {
                    winner = 0;
                }
                else if (scoreP2 > scoreP1)
                {
                    winner = 1;
                }
                else
                { 
                    winner = -1; 
                }
                GameOverRpc(winner);
            }
            else
            {
                turnOrder = !turnOrder;
                BroadcastTurnChange(turnOrder ? 0 : 1);
                RollDice();
            }
        }

        public void HandleRematch(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex > 1) return;

            if (!isGameOver)
            {
                Console.WriteLine($"Player {playerIndex} requested rematch but game is not over.");
                return;
            }
            if (playerIndex == 0) p1WantsRematch = true;
            else p2WantsRematch = true;

            Console.WriteLine($"Room: Player {playerIndex} requested rematch.");

            if (p1WantsRematch && p2WantsRematch)
            {
                Console.WriteLine("Room: Both players requested rematch. Restarting game...");
                ResetGame();
                gameStarted = true;
                BroadcastStartGame();
                RollDice();
            }
        }

        public void HandleDisconnect(TcpNetworkConnection connection)
        {
            int index = GetPlayerIndex(connection);
            if (index <0) return;
            
            Console.WriteLine($"Room: Player {index} disconnected.");
            OSCMessageOut msg = new OSCMessageOut("/OpponentDisconnected");

            moveTimerPaused = true;
            if (index ==0)
            {
                p1Disconnected = true;
                p1DisconnectTime = DateTime.Now;
                player1 = null;
            }
            else
            {
                p2Disconnected = true;
                p2DisconnectTime = DateTime.Now;
                player2 = null;
            }
            TcpNetworkConnection opponent = GetOpponent(index);
            if (opponent != null)
            {
                opponent.Send(msg.GetBytes());
            }
        }
        public void HandleLeave(TcpNetworkConnection connection)
        {
            int index = GetPlayerIndex(connection);
            if (index < 0) return;

            Console.WriteLine($"Player {index} intentionally left.");

            OSCMessageOut msg = new OSCMessageOut("/OpponentLeft");

            TcpNetworkConnection opponent = GetOpponent(index);
            if (opponent != null)
            {
                opponent.Send(msg.GetBytes());
            }

            ResetConnectionState();
            player1 = null;
            player2 = null;
            forceEmpty = true;
        }
        #endregion

        #region Broadcasting 

        public void Broadcast(byte[] data)
        {
            if (player1 != null && player1.Status == ConnectionStatus.Connected)
            {
                player1.Send(data);
            }
            if (player2 != null && player2.Status == ConnectionStatus.Connected)
            {
                player2.Send(data);
            }

        }
        void RollDice()
        {
            Random rng = new Random();
            dice1 = rng.Next(1, 7);
            dice2 = rng.Next(1, 7);
            BroadcastDiceRolled(dice1, dice2);
        }
        void BroadcastDiceRolled(int d1, int d2)
        {
            OSCMessageOut msg = new OSCMessageOut("/DiceRolled")
                .AddInt(d1)
                .AddInt(d2);
            Broadcast(msg.GetBytes());
        }
        void BroadcastStartGame()
        {
            OSCMessageOut msg = new OSCMessageOut("/StartGame");
            Broadcast(msg.GetBytes());
        }

        void BroadcastTurnChange(int player)
        {
            moveTimeRemaining = MoveTimeoutSeconds;
            moveTimerPaused = false;
            lastTickTime = DateTime.Now;
            lastSentMoveCountdown = -1;
            OSCMessageOut msg = new OSCMessageOut("/TurnChanged")
                .AddInt(player);

            Broadcast(msg.GetBytes());
        }
        void BroadcastGridUpdate(int playerIndex, int col)
        {
            Model model;
            if (playerIndex == 0)
            {
                model = p1Model;

            }
            else
            {
                model = p2Model;
            }

            for (int row = 0; row < 3; row++)
            {
                int value = model.grid[row, col];
                OSCMessageOut msg = new OSCMessageOut("/GridUpdated")
                    .AddInt(playerIndex)    
                    .AddInt(row)
                    .AddInt(col)
                    .AddInt(value);

                Broadcast(msg.GetBytes());
            }
        }

        void BroadcastScoreUpdate()
        {
            int s1 = p1Model.CalculateGridScore();
            int s2 = p2Model.CalculateGridScore();

            OSCMessageOut msg = new OSCMessageOut("/ScoreUpdated")
                .AddInt(s1)
                .AddInt(s2);

            Broadcast(msg.GetBytes());
        }

        

        public void GameOverRpc(int winner)
        {
            isGameOver = true;
            OSCMessageOut message = new OSCMessageOut("/GameOver").AddInt(winner);
            Broadcast(message.GetBytes());
        }

        #endregion

        #region Player info and full board state

        public void SendPlayerInfo(TcpNetworkConnection connection)
        {
            int index = GetPlayerIndex(connection);
            if (index < 0) return;

            OSCMessageOut msg = new OSCMessageOut("/PlayerInfo")
                .AddInt(index);
            connection.Send(msg.GetBytes());
        }

        

        public void SendFullState(TcpNetworkConnection connection)
        {
            int index = GetPlayerIndex(connection);

            OSCBundleOut bundle = new OSCBundleOut(0);

            bundle.AddMessage(new OSCMessageOut("/PlayerInfo").AddInt(index));

            bundle.AddMessage( new OSCMessageOut("/DiceRolled").AddInt(dice1).AddInt(dice2));
         
            Model myModel;
            if(index == 0)
            {
                myModel = p1Model;
            }
            else
            {
                myModel = p2Model;
            }
            
            for (int col = 0; col < 3; col++)
            {
                for (int row = 0; row < 3; row++)
                {
                    int value;
                    value = myModel.grid[row, col];

                    bundle.AddMessage(new OSCMessageOut("/GridUpdated").AddInt(index).AddInt(row).AddInt(col).AddInt(value));
                }
            }

            int opponent;
            Model opponentModel;
            if (index == 0)
            {
                opponent = 1;
                opponentModel = p2Model;
            }
            else
            {
                opponent = 0;
                opponentModel = p1Model;
            }
            for (int col = 0; col < 3; col++)
            {
                for (int row = 0; row < 3; row++)
                {
                    int value = opponentModel.grid[row, col];
                    bundle.AddMessage(new OSCMessageOut("/GridUpdated").AddInt(opponent).AddInt(row).AddInt(col).AddInt(value));                
                }
            }

            bundle.AddMessage(new OSCMessageOut("/ScoreUpdated").AddInt(p1Model.CalculateGridScore()).AddInt(p2Model.CalculateGridScore()));

            if (CheckGameOver(myModel.grid) || CheckGameOver(opponentModel.grid))
            {
                int scoreP1 = p1Model.CalculateGridScore();
                int scoreP2 = p2Model.CalculateGridScore();

                int winner;
                if (scoreP1 > scoreP2) winner = 0;
                else if (scoreP2 > scoreP1) winner = 1;
                else winner = -1; 

                bundle.AddMessage(new OSCMessageOut("/GameOver").AddInt(winner));

                connection.Send(bundle.GetBytes());
                return;
            }

            bundle.AddMessage(new OSCMessageOut("/TurnChanged").AddInt(turnOrder ? 0 : 1));
            connection.Send(bundle.GetBytes());
        }
        #endregion
    }
}
