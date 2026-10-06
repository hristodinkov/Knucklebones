# Knucklebones

A two-player, turn-based dice game built in Unity with a **server-authoritative multiplayer** architecture. The project follows the **MVC (Model-View-Controller)** pattern, adapted for a client-server setup.  Each player has a 3x3 grid. On your turn, two dice are rolled, you pick one and place it in a column. Matching dice in a column multiply your score, and placing a die destroys your opponent's dice of the same value in that column. The game runs on a standalone **C# console server** that handles rooms, turn order, validation, timers and reconnection, while the Unity client only sends choices and renders what the server tells it. All communication uses **OSC (Open Sound Control) messages over TCP**, and game traffic is limited to plain integers (dice values, rows, columns, scores).

## Preview
<img src="Pictures/gameplay.gif" width="600" height="600">

## Game Rules

* Each player has a 3x3 grid made of 3 columns
* Every turn, two dice are rolled and the active player picks one of them
* The chosen die is placed in a column of the player's own grid (it falls into the first free row)
* All dice with the same value in the **opponent's matching column** are destroyed
* Column score:
  * All different: the sum of the dice
  * Two matching: the matching pair counts double
  * Three matching: the sum counts triple
* The game ends when one player's grid is full. The highest total score wins, equal scores are a draw

## Architecture

| Part | Description |
|------|-------------|
| **1. Dedicated Server** | A standalone console app listening on port `50001`. It accepts connections, assigns players to rooms, and runs every `GameRoom` each tick. | 
| **2. Game Rooms** | Each room holds two players, the authoritative `Model` grids, dice rolls, the turn order and the timers. Rooms are created automatically and cleaned up when empty. | 
| **3. Unity Client** | `Client` handles the TCP connection and OSC messages, then raises C# events. `Controller` sends player input, while `View` and `UIView` subscribe to the events to render dice, scores, turns and windows. | 
| **4. Input & Selection** | `CameraClickDetector` raycasts the mouse click, and `Selectable` marks dice and columns. `SelectedDiceVisual` and `SelectColumn` highlight the chosen die and the valid columns. |

## MVC Pattern

The project separates game data, input handling and presentation. Because the game is networked, the Model lives on the server, and the client keeps a lightweight copy of it.

| Role | Classes | Responsibility |
|------|---------|----------------|
| **Model** | `Model` (server), `LocalClientModel` (client) | `Model` is the authoritative 3x3 grid: dice placement, removal of matching dice, column and total score. `LocalClientModel` is the client's copy of the grid values, updated only by server messages. |
| **View** | `View`, `UIView`, `SelectColumn`, `SelectedDiceVisual` | `View` spawns and destroys dice in the grids. `UIView` shows scores, turn, timers and the game over / disconnect windows. `SelectColumn` and `SelectedDiceVisual` highlight the selected die and valid columns. Views never change game state, they only react to events. |
| **Controller** | `Controller` | Receives player input (choose die, choose column, rematch, leave) and forwards it to the server through `Client`. It does not modify the grid, because the server decides whether a move is valid. |

### How the pieces communicate
* **Controller to Model:** the controller sends an *intent* (`/ChooseColumn`) to the server, and the server validates it and updates the `Model`.
* **Model to View:** the server broadcasts the result, `Client` updates the `LocalClientModel` and raises C# events such as `OnGridUpdated`, `OnScoreUpdated` and `OnTurnChanged`. The views subscribe to these events (Observer pattern), so the View never polls the Model.
* **Decoupling:** `View` and `UIView` have no knowledge of the network or the server, only of the `Client` events. The same `Model` class also runs on the console server without any Unity dependency.

### Network Messages (OSC over TCP)

| Direction | Message | Purpose |
|-----------|---------|---------|
| Client → Server | `/RequestPlayerID` (token) | Join a room or reconnect with a saved token |
| Client → Server | `/ChooseDice` (index) | Select one of the two rolled dice |
| Client → Server | `/ChooseColumn` (column) | Place the selected die |
| Client → Server | `/RequestRematch`, `/LeaveRoom` | Rematch voting and leaving the match |
| Server → Client | `/PlayerInfo`, `/TurnChanged` | Which player you are and whose turn it is |
| Server → Client | `/DiceRolled`, `/GridUpdated`, `/ScoreUpdated` | Board state updates |
| Server → Client | `/StartGame`, `/GameOver` | Match start (and rematch) and the winner |
| Server → Client | `/OpponentDisconnected`, `/OpponentReconnected`, `/OpponentLeft` | Opponent connection state |
| Server → Client | `/ReconnectCountdown`, `/MoveCountdown`, `/Ping` | Timers and keep-alive |

## Features

### Multiplayer
* Dedicated server with automatic room creation and matchmaking
* Server-authoritative game logic, so clients cannot place dice directly
* Server-side validation of dice index, column index, turn order and selected die
* Periodic ping from the server to detect dead connections

### Reconnection & Match Flow
* Persistent reconnection token (stored in `PlayerPrefs`), so a player can rejoin their room after a disconnect
* Reconnect countdown and an "opponent disconnected" window
* Per-move countdown timer shown in the UI
* "Opponent left" window when a player leaves on purpose
* Rematch system that restarts the game when both players agree

### Gameplay & UI
* Click-to-select dice and columns
* Highlighted selected die and flashing valid columns for the active player
* Live score, turn and timer display
* Game over window with winner or draw

## Controls
* Left Click on a die: select it
* Left Click on a column of your grid: place the selected die

## Requirements
* Unity 6000.5.3f1 or newer
* Unity Input System package
* TextMeshPro
* .NET SDK (to build and run the console server)

## Installation

### 1. Start the server
* Clone the repository
* Open the server project (`Program.cs`, `GameRoom.cs`) in your IDE
* Build and run it. It listens on port `50001`
* Press `Q` in the console to stop it

### 2. Start the clients
* Open the Unity project
* Open the scene located in `Assets/Scenes/Knucklebones.unity`
* Press Play, or build the game and run a second instance
* The first two clients that connect are placed in the same room and the game starts

### Testing with two instances on one PC
* Run one client in the editor and one as a build
* In a build you can pass `-token=<your-token>` as a launch argument to force a specific reconnection token. In the editor a new token is generated on every Play

## Known Issues

* LAN server discovery (`LanDiscovery`, `LanUI`) is unfinished. The client currently connects to `ServerIP` (default is localhost)

## Future Improvements

* Finish LAN discovery and let players pick a server from the UI
* Bluetooth connection between devices 
