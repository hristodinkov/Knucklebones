using NetworkConnections;
using OSCTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TcpEchoServerPolling
{
    class GameRoom
    {
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

        public string p1Token;
        public string p2Token;

        private static readonly TimeSpan ReconnectWindow = TimeSpan.FromSeconds(60);

        private const float ReconnectWindowSeconds = 30f; 

        public DateTime p1DisconnectTime;
        public DateTime p2DisconnectTime;
        public bool p1Disconnected = false;
        public bool p2Disconnected = false;
        private bool forceEmpty = false;
        private bool isGameOver = false;
        public bool gameStarted = false;

        private float countdownBroadcastAccumulator = 0f;

        float lastPingTime = 0; // combine this with some room.update method?

        public GameRoom()
        {
            ResetGame();
        }
        public bool IsFull() {
            return player1 != null && player2 != null;
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
                gameStarted = true;
                ResetGame();
                Console.WriteLine("Room full,starting game...");
                BroadcastStartGame();
                turnOrder = true;
                BroadcastTurnChange(0);
                RollDice();
            }
        }
        public bool Contains(TcpNetworkConnection connection)
        {
            return player1 == connection || player2 == connection;
        }
       
        public bool IsEmpty()
        {
            if (forceEmpty) return true;

            bool p1Gone = player1 == null || player1.Status == ConnectionStatus.Disconnected;
            bool p2Gone = player2 == null || player2.Status == ConnectionStatus.Disconnected;

            if (!p1Gone || !p2Gone) return false;

            bool p1Expired = !p1Disconnected || (DateTime.Now - p1DisconnectTime).TotalSeconds >= ReconnectWindowSeconds;
            bool p2Expired = !p2Disconnected || (DateTime.Now - p2DisconnectTime).TotalSeconds >= ReconnectWindowSeconds;

            if (p1Expired && p2Expired)
            {
                p1Token = null;
                p2Token = null;
                return true;
            }

            return false;
        }

        

        public void Tick(float deltaMs)
        {
            if(isGameOver||forceEmpty)
            {
                if (p1Disconnected && !p2Disconnected)
                {
                    ReconnectionTimeout(deltaMs, 0);
                }
                else if (p2Disconnected && !p1Disconnected)
                {
                    ReconnectionTimeout(deltaMs, 1);
                }
                else
                {
                    countdownBroadcastAccumulator = 0f;
                }
            }
            
        }

        private void ReconnectionTimeout(float deltaMs,int playerIndex)
        {
            DateTime disconnectTime;
            TcpNetworkConnection opponent;

            if (playerIndex == 0)
            {
                disconnectTime = p1DisconnectTime;
                opponent = player2;
            }
            else
            {
                disconnectTime = p2DisconnectTime;
                opponent=player1;
            }
            float elapsed = (float)(DateTime.Now - disconnectTime).TotalSeconds;
            float remaining = ReconnectWindowSeconds - elapsed;

            countdownBroadcastAccumulator += deltaMs;
            if (countdownBroadcastAccumulator >= 1000f) // every 1 second
            {
                countdownBroadcastAccumulator = 0f;
                int remainingInt = Math.Max(0, (int)remaining);
                OSCMessageOut countdown = new OSCMessageOut("/ReconnectCountdown").AddInt(remainingInt);
                opponent.Send(countdown.GetBytes());
            }

            if (remaining <= 0)
            {
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

        public void ResetGame()
        {
            p1Model = new Model(1);
            p2Model = new Model(2);
            selectedDiceP1 = -1;
            selectedDiceP2 = -1;
            turnOrder = true;
            p1Disconnected = false;
            p2Disconnected = false;
            isGameOver = false;
            forceEmpty = false;
            gameStarted = false;

        }
        public int GetPlayerIndex(TcpNetworkConnection connection)
        {
            if (connection == player1)
            {
                return 0;
            }
            if (connection == player2)
            {
                return 1;
            }
            return -1;
        }

        //----Incomign message handling methods----

        public void HandleChooseDice(int playerIndex, int diceIndex)
        {
            int chosenDice;
            if (diceIndex == 0)
            {
                chosenDice = dice1;
            }
            else
            {
                chosenDice = dice2;
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

            bool success = add.TryAddNewDice(selected, col);
            if (!success) return;

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
                return;
            }
            else
            {
                turnOrder = !turnOrder;
                BroadcastTurnChange(turnOrder ? 0 : 1);
                RollDice();
            }
        }

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

        public void HandleRematch(int playerIndex)
        {
            if (playerIndex == 0) p1WantsRematch = true;
            else p2WantsRematch = true;

            Console.WriteLine($"Room: Player {playerIndex} requested rematch.");

            if (p1WantsRematch && p2WantsRematch)
            {
                Console.WriteLine("Room: Both players requested rematch. Restarting game...");
                ResetGame();
                BroadcastStartGame();
                RollDice();
            }
        }

        public void HandleDisconnect(TcpNetworkConnection connection)
        {
            int index = GetPlayerIndex(connection);
            Console.WriteLine($"HandleDisconnect called, GetPlayerIndex returned {index}");
            if (index <0)
            {
                return;
            }

            Console.WriteLine($"Room: Player {index} disconnected.");
            Console.WriteLine($"Sending OpponentDisconnected to player {(index == 0 ? 1 : 0)}");
            OSCMessageOut msg = new OSCMessageOut("/OpponentDisconnected");
            countdownBroadcastAccumulator = 0f;

            if (index == 0 && player2 != null)
                player2.Send(msg.GetBytes());
            else if (index == 1 && player1 != null)
                player1.Send(msg.GetBytes());

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
            

            
        }
        public void HandleLeave(TcpNetworkConnection connection)
        {
            int index = GetPlayerIndex(connection);
            if (index < 0) return;

            Console.WriteLine($"Player {index} intentionally left. Opponent wins.");

            OSCMessageOut msg = new OSCMessageOut("/OpponentLeft");
            if (index == 0) player2?.Send(msg.GetBytes());
            else player1.Send(msg.GetBytes());

            p1Token = null;
            p2Token = null;
            p1Disconnected = false;
            p2Disconnected = false;
            player1 = null;
            player2 = null;
            forceEmpty = true;

        }

        //----Game event broadcasting methods----
        void RollDice()
        {
            Random rng = new Random();
            dice1 = rng.Next(1, 7);
            dice2 = rng.Next(1, 7);
            BroadcastDiceRolled(dice1, dice2);
        }
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

        void BroadcastStartGame()
        {
            OSCMessageOut msg = new OSCMessageOut("/StartGame");
            Broadcast(msg.GetBytes());
        }

        void BroadcastDiceRolled(int d1, int d2)
        {
            OSCMessageOut msg = new OSCMessageOut("/DiceRolled")
                .AddInt(d1)
                .AddInt(d2);
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

        void BroadcastTurnChange(int player)
        {
            OSCMessageOut msg = new OSCMessageOut("/TurnChanged")
                .AddInt(player);

            Broadcast(msg.GetBytes());
        }

        public void GameOverRpc(int winner)
        {
            isGameOver = true;
            OSCMessageOut message = new OSCMessageOut("/GameOver").AddInt(winner);
            Broadcast(message.GetBytes());
        }

        public void SendPlayerInfo(TcpNetworkConnection connection)
        {
            int index = GetPlayerIndex(connection);
            if (index < 0) return;

            OSCMessageOut msg = new OSCMessageOut("/PlayerInfo")
                .AddInt(index);
            connection.Send(msg.GetBytes());
        }

        public void ReconnectPlayer(TcpNetworkConnection connection, int index)
        {
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

            if (index == 0 && player2 != null)
                player2.Send(msg.GetBytes());
            else if (index == 1 && player1 != null)
                player1.Send(msg.GetBytes());

            SendFullState(connection);
            countdownBroadcastAccumulator = 0f;
        }

        public void SendFullState(TcpNetworkConnection connection)
        {
            int index = GetPlayerIndex(connection);

            OSCBundleOut bundle = new OSCBundleOut(0);

            bundle.AddMessage(new OSCMessageOut("/PlayerInfo").AddInt(index));

            bundle.AddMessage( new OSCMessageOut("/DiceRolled").AddInt(dice1).AddInt(dice2));
         
            Model playerModel;
            if(index == 0)
            {
                playerModel = p1Model;
            }
            else
            {
                playerModel = p2Model;
            }
            
            for (int col = 0; col < 3; col++)
            {
                for (int row = 0; row < 3; row++)
                {
                    int value;
                    value = playerModel.grid[row, col];

                    bundle.AddMessage(new OSCMessageOut("/GridUpdated").AddInt(index).AddInt(row).AddInt(col).AddInt(value));
                }
            }

            int opponent;
            if (index == 0)
            {
                opponent = 1;
            }
            else
            {
                opponent = 0;
            }
            Model opponentModel;
            if (index == 0)
            {
                opponentModel = p2Model;
            } 
            else
            {
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

            if (CheckGameOver(playerModel.grid) || CheckGameOver(opponentModel.grid))
            {
                int scoreP1 = p1Model.CalculateGridScore();
                int scoreP2 = p2Model.CalculateGridScore();

                int winner;
                if (scoreP1 > scoreP2) winner = 0;
                else if (scoreP2 > scoreP1) winner = 1;
                else winner = -1; // tie

                bundle.AddMessage(new OSCMessageOut("/GameOver").AddInt(winner));

                connection.Send(bundle.GetBytes());
                return;
            }

            bundle.AddMessage(new OSCMessageOut("/TurnChanged").AddInt(turnOrder ? 0 : 1));
            connection.Send(bundle.GetBytes());
        }
    }
}
