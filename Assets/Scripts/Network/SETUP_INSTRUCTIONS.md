# 🎮 Battleship Online Multiplayer – Setup Instructions

> **Networking solution chosen: [Mirror](https://mirror-networking.com/)**
> Mirror is the most beginner-friendly option for Unity multiplayer.
> It uses simple `[Command]` (client → server) and `[ClientRpc]` / `[TargetRpc]` (server → client) attributes.

---

## 📦 Step 0 – Install Mirror

The `manifest.json` has already been updated with a git-URL reference.
When you open the project in Unity the Package Manager will download Mirror
automatically.

**Alternative (Asset Store):** Open **Window → Package Manager**, click the
**+** button, choose **Add package from git URL…**, and paste:

```
https://github.com/MirrorNetworking/Mirror.git?path=/Assets/Mirror#v89.8.0
```

Or download Mirror free from the **Unity Asset Store** (search "Mirror").

After installation you should see a `Mirror` folder under Packages (or Assets
if installed via Asset Store).

---

## 🏗️ Step 1 – Scene setup overview

All new GameObjects are created in the **Battle** scene.

| New GameObject | Components to add | Purpose |
|---|---|---|
| `NetworkManager` | `BattleshipNetManager`, `Kcp Transport` | Connection management, server game logic |
| `PlayerPrefab` *(prefab)* | `NetworkIdentity`, `BattleshipPlayer` | Spawned per player, handles Commands/RPCs |
| `LobbyUI` | `LobbyUI` (script) | Host/Join buttons, turn HUD |
| `NetworkBoardSetup` | `NetworkBoardSetup` (script) | Board visuals & click management |

---

## 🔧 Step 2 – Create the NetworkManager GameObject

1. In the **Battle** scene Hierarchy, right-click → **Create Empty**. Name it
   `NetworkManager`.
2. With it selected, click **Add Component** in the Inspector:
   - Search for **Kcp Transport** and add it.
   - Search for **BattleshipNetManager** and add it.
3. In the **BattleshipNetManager** component:
   - Drag the **Kcp Transport** component into the **Transport** field.
   - Leave *Ship Lengths* as default `[5, 4, 3, 3, 2]` (standard Battleship)
     or customise to match your ship setup.
   - **Player Prefab** → will be set in Step 3.

> **Important:** There must be exactly **one** NetworkManager in the scene.
> If you already have a `NetworkManager` from Mirror's default, remove it.

---

## 🧑 Step 3 – Create the Player Prefab

1. In the Hierarchy, right-click → **Create Empty**. Name it `PlayerPrefab`.
2. Add these components:
   - **NetworkIdentity** (from Mirror).
   - **BattleshipPlayer** (your script).
3. Drag `PlayerPrefab` from the Hierarchy into **Assets/Prefabs** (create the
   folder if needed) to make it a **prefab**.
4. **Delete** the `PlayerPrefab` instance from the Hierarchy (the prefab in
   the Project window is all we need).
5. Select the `NetworkManager` GameObject, and drag the `PlayerPrefab` prefab
   into the **Player Prefab** slot of `BattleshipNetManager`.

---

## 🖥️ Step 4 – Create the Lobby UI

1. In the Hierarchy, right-click → **UI → Canvas**. Name it `NetworkCanvas`.
   (Or reuse an existing Canvas.)
2. Inside `NetworkCanvas`, create:
   - **UI → Panel** → name it `LobbyPanel`.
   - Inside `LobbyPanel`:
     - **UI → Button – TextMeshPro** → name it `HostButton` (label: *"Host Game"*).
     - **UI → Button – TextMeshPro** → name it `JoinButton` (label: *"Join Game"*).
     - **UI → Input Field – TextMeshPro** → name it `IpInput`
       (placeholder: *"Enter host IP…"*).
     - **UI → Text – TextMeshPro** → name it `StatusText`.
3. Outside `LobbyPanel` but still inside `NetworkCanvas`:
   - **UI → Text – TextMeshPro** → name it `TurnText`.
4. Create an empty GameObject `LobbyUI`, add the **LobbyUI** script.
5. In the Inspector, drag the references:
   - `_lobbyPanel` ← `LobbyPanel`
   - `_hostButton` ← `HostButton`
   - `_joinButton` ← `JoinButton`
   - `_ipAddressInput` ← `IpInput`
   - `_statusText` ← `StatusText`
   - `_turnText` ← `TurnText`

---

## 🗺️ Step 5 – Set up NetworkBoardSetup

1. Create an empty GameObject named `NetworkBoardSetup`.
2. Add the **NetworkBoardSetup** script.
3. In the Inspector:
   - `_boards` array size = **2**.
   - `_boards[0]` ← drag your **left board** (Player 0's board).
   - `_boards[1]` ← drag your **right board** (Player 1's board).
4. *(Optional)* Assign a `_shipIndicatorMaterial` to visually mark ship tiles
   on the defence board. If left empty, a dark-grey tint is applied.

---

## 🎯 Step 6 – Set Board Index on each Board

1. Select **Board 0** (left board) in the Hierarchy.
2. In the **BoardGenerator** component, set **Board Index = 0**.
3. Select **Board 1** (right board) in the Hierarchy.
4. In **BoardGenerator**, set **Board Index = 1**.

> This lets each tile know which board it belongs to, so the network layer
> can route attacks correctly.

---

## 🧪 Step 7 – Test locally

1. **Build & Run**: File → Build Settings → **Build And Run**.
2. In the built player window, click **Host Game**.
3. Back in the Unity Editor, click **Play** and then **Join Game** (leave IP
   as `localhost`).
4. You should see:
   - Both players connected.
   - Ship indicators on each player's defence board.
   - Turn text showing whose turn it is.
   - Clicking tiles on the opponent's board sends attacks.
   - Hits/misses update on both screens.

> **Tip**: Use *ParrelSync* (free Unity tool) to run two editor instances
> side-by-side for faster testing.

---

## 🌐 Step 8 – Play over the internet

1. The **Host** player clicks **Host Game**. They need to know their public IP
   (or use a service like [ngrok](https://ngrok.com/) to tunnel port **7777**).
2. The **Client** types the Host's IP address into the `IpInput` field and
   clicks **Join Game**.
3. Make sure port **7777 (UDP)** is forwarded on the Host's router.

---

## 📁 File summary

| File | Type | Description |
|---|---|---|
| `Assets/Scripts/Network/BattleshipNetManager.cs` | **NEW** | Custom NetworkManager – connections, server boards, turns, attack validation |
| `Assets/Scripts/Network/BattleshipPlayer.cs` | **NEW** | Per-player Commands & RPCs |
| `Assets/Scripts/Network/LobbyUI.cs` | **NEW** | Lobby buttons + turn HUD |
| `Assets/Scripts/Network/NetworkBoardSetup.cs` | **NEW** | Board visuals & click toggling |
| `Assets/Scripts/Tiles and Ships/Tile.cs` | **MODIFIED** | Added `SetCoordinates`, `IsNetworked`, `NetworkApplyHit/Miss/MarkAsShip`, `HandleNetworkClick` |
| `Assets/Scripts/Board/BoardGenerator.cs` | **MODIFIED** | Added `BoardIndex` property; passes coordinates to tiles |
| `Assets/Scripts/General/GameManager.cs` | **MODIFIED** | Skips local ship placement when a `BattleshipNetManager` is detected |
| `Packages/manifest.json` | **MODIFIED** | Added Mirror git-URL dependency |

---

## ⚙️ Architecture overview

```
┌─────────────┐         [Command] CmdAttack(x,z)         ┌──────────────────┐
│   CLIENT A   │ ──────────────────────────────────────▶  │                  │
│ (BattleshipPlayer)                                       │  SERVER (Host)   │
│              │  ◀────────────────────────────────────── │ BattleshipNet-   │
│   CLIENT B   │    [ClientRpc] RpcAttackResult(...)       │ Manager          │
│ (BattleshipPlayer)                                       │                  │
└─────────────┘                                           │ • int[,] board0  │
       ▲                                                  │ • int[,] board1  │
       │                                                  │ • turn logic     │
  NetworkBoardSetup                                       │ • ship health    │
  updates tile visuals                                    └──────────────────┘
```

**Turn flow:**
1. Player clicks tile → `Tile.HandleNetworkClick()` → `BattleshipPlayer.RequestAttack(x,z)`
2. `CmdAttack` sent to server.
3. Server checks board data: Hit or Miss.
4. Server broadcasts `RpcAttackResult` to both players.
5. `NetworkBoardSetup` updates tile visuals.
6. On miss: server calls `RpcTurnChanged` to switch turns.
7. On hit: same player continues. If all ships sunk → `RpcGameOver`.

---

## ❓ FAQ

**Q: Do I need to add NetworkIdentity to the Boards or Tiles?**
A: **No.** Only the Player Prefab needs `NetworkIdentity`. Boards and tiles
are local scene objects that receive visual updates via events.

**Q: Can I still play the local (hotseat) version?**
A: Yes. If there is no `NetworkManager` in the scene, `GameManager` runs the
original local setup. Just remove or disable the `NetworkManager` GameObject.

**Q: My tiles don't respond to clicks.**
A: Check that the opponent's board tiles are on the `GameBoard` layer (or
whatever layer your camera's Physics Raycaster uses). `NetworkBoardSetup`
toggles layers to enable/disable clicking.

**Q: How do I change the number or size of ships?**
A: Edit the `_shipLengths` array in the `BattleshipNetManager` Inspector
(e.g., `[5, 4, 3, 3, 2]` for standard Battleship).
