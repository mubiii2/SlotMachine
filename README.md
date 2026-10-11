# 🎰 Classic Casino Slot Machine (Unity 6 / WebGL)

An interactive, responsive 3-reel retro casino slot machine developed in **Unity 6 (6000.3.19f1)** adhering to strict Object-Oriented Programming (OOP) principles, clean architecture, fair weighted Random Number Generation (RNG), and realistic mechanical reel physics.

🎮 **[Play Live WebGL Demo on Unity Play](https://play.unity.com/en/games/b54c2347-a4b3-42d3-8a46-07c235912685/webgl)**

---

## 🎮 Live Game Overview & Features

### Core Mechanics
- **3-Reel Classic Format**: 3 visible rows (Top, Middle, Bottom) across 3 spinning reels.
- **Payline Evaluation**:
  - **Center Payline (Middle Row)**: Matches evaluate across the middle horizontal payline, exactly following classic single-line slot mechanics.
  - Matches across all 3 reels on the center row award the symbol multiplier.
  - 3 Bonus Bells on the center row trigger Free Spins.
- **Winning Combinations & Multipliers**:
  - ⭐ **WILD Star**: **150x Bet** (Substitutes for *all* symbols!)
  - 7️⃣ **Lucky 7 (Jackpot)**: **100x Bet** (Mega Jackpot celebration)
  - 🔔 **Bell (Bonus Trigger)**: **50x Bet** (3 Bells trigger Free Spins!)
  - 🍫 **BAR**: **25x Bet**
  - 🍒 **Cherry**: **10x Bet** for 3 cherries, **2x Bet** for 2 cherries
- **Player Bankroll & Economy**:
  - Starting bankroll: **1,000 Credits**
  - Bet Selector: **5, 10, 25, 50, 100** credits per spin
  - Max Bet shortcut button
  - Automated Refill / Loan prompt when credits run out!

### 💫 Reel Animation & Game Feel
- **Anticipation Windup**: Reels slightly kick back upward prior to launching downward, simulating spring tension.
- **Continuous High-Speed Spin**: Smooth, seamless downward loop with buffer recycling.
- **Staggered Suspense Stops**: Reel 1 locks first, followed by Reel 2 (+0.35s), and Reel 3 (+0.70s) for authentic casino suspense.
- **Damped Elastic Bounce**: Rebounds past the resting stop and settles with harmonic damped oscillation.
- **Dynamic Win Ticker**: Payout numbers rapidly count up with matching sound cues.
- **Visual Highlight Grid**: Winning payline symbols pulse and illuminate upon winning combinations.

### 🌟 Creative Bonus Features
1. **Interactive Mechanical Lever**:
   - Clickable lever arm that pulls down (`slot-machine3.png`) with mechanical audio and springs back up smoothly (`slot-machine2.png`).
2. **Wild Substitution**:
   - The Wild Star symbol substitutes for any symbol to form winning combinations on any payline.
3. **Free Spins Bonus Mode (2x Multiplier)**:
   - 3 Bell/Bonus symbols trigger **5 Free Spins**.
   - During Free Spins, spins do not deduct balance, and all payline winnings are **doubled (2x)**!
4. **Auto-Spin Feature**:
   - Toggleable hands-free auto spin with state awareness and automatic safety stop when balance is depleted.
5. **Interactive Paytable & Rules Modal**:
   - In-game popup window detailing all symbol multipliers, bonus conditions, and rules.
6. **Procedural & Synthesized Audio**:
   - Authentic retro audio effects (lever ratchet, reel spin hum, reel stop lock, coin ping, win chimes, jackpot fanfare) synthesized via `AudioClip.Create` to eliminate runtime audio codec issues on WebGL.

---

## 🏗️ Architecture & Technical Design

The codebase is organized into distinct domain layers adhering to SOLID design principles:

```
Assets/
├── Art/
│   └── Sprites/               # Pre-sliced UI buttons, machine frame, reel backdrops, symbols
├── Audio/                     # Sound clips and runtime synthesizers
├── Scenes/
│   └── SlotMachineScene.unity # Master game scene
├── Scripts/
│   ├── Core/
│   │   ├── Enums.cs           # SymbolId, SlotState, PaylineId
│   │   └── SymbolDefinition.cs# Configurable symbol metadata, weights, multipliers
│   ├── RNG/
│   │   ├── IRandomNumberGenerator.cs # RNG interface for dependency inversion & testability
│   │   └── WeightedRNG.cs     # Cryptographically seeded, cumulative probability distribution
│   ├── Reels/
│   │   ├── ReelSymbol.cs      # Individual symbol visual slot & highlight
│   │   ├── Reel.cs            # Physics loop: ease-in, steady loop, deceleration, elastic bounce
│   │   └── ReelManager.cs     # Multi-reel coordination & staggered stop sequencing
│   ├── Gameplay/
│   │   ├── PaylineEvaluator.cs# Pure logic matrix evaluation (Wild substitution, paylines, scatters)
│   │   ├── PlayerWallet.cs    # Balance, bets, win tracking, free spins counter
│   │   └── SlotMachineController.cs # Master game coordinator connecting state machine
│   ├── Audio/
│   │   └── SoundManager.cs    # Channel-based audio controller with procedural fallback
│   ├── UI/
│   │   ├── UIManager.cs       # Text bindings, button listeners, win ticker, banner overlays
│   │   ├── LeverController.cs # Interactive lever pointer click & spring animation
│   │   └── PaytableModal.cs   # Popup dialog controller
│   └── Editor/
│       ├── SlotMachineSceneBuilder.cs # Automated scene constructor & sprite configuration
│       └── SlotMachineBuildScript.cs  # Automated headless WebGL builder
└── Build/
    └── WebGL/                 # Ready-to-play WebGL distribution
```

---

## 🕹️ Instructions to Run WebGL Build Locally

Because WebGL builds run inside the browser and require local file access via HTTP/HTTPS (to avoid CORS `file://` protocol restrictions), run using any local static web server:

### Option 1: Python HTTP Server (Recommended)
```bash
# Navigate to the WebGL build directory
cd Build/WebGL

# Start local server on port 8000
python -m http.server 8000
```
Then open your browser at:
👉 **`http://localhost:8000`**

### Option 2: Node.js / npx
```bash
cd Build/WebGL
npx serve .
```

### Option 3: Unity Editor Play Mode
Open the project in Unity 6, open `Assets/Scenes/SlotMachineScene.unity`, and press **Play** (▶️).

---

## 🧪 RNG Verification & Fairness

The RNG implements `IRandomNumberGenerator` backed by `WeightedRNG`:
- **Entropy Source**: `System.Security.Cryptography.RandomNumberGenerator` 32-bit seeds.
- **Fairness & Odds Distribution**:
  - `Cherry` (Weight: 38) ~ 40% occurrence (Frequent small payouts)
  - `BAR` (Weight: 26) ~ 28% occurrence (Medium payouts)
  - `Bell` (Weight: 16) ~ 17% occurrence (High payout & Free Spin trigger)
  - `Lucky 7` (Weight: 8) ~ 8.5% occurrence (Jackpot payout)
  - `Wild Star` (Weight: 6) ~ 6.5% occurrence (Substitutes any symbol)
- **Deterministic Testing**: Supports constructor seed injection `new WeightedRNG(seed)` for automated regression testing and validation of win paylines.

---

## 📝 Thought Process & Approach

1. **Asset Decomposition & Pixel-Perfect Alignment**:
   - Analyzed the asset kit dimensions (`slot-machine1` through `slot-machine5` at 816x624).
   - Positioned the transparent reel window `[229, 246]` to `[596, 455]` with a `RectMask2D` container to cleanly clip spinning symbols.
   - Sliced composite button sheets into distinct sprites to support responsive state changes (normal, hover, pressed).
2. **Game Feel First**:
   - In slot games, anticipation is key. Adding a 0.2s reverse windup, followed by high-speed spinning and staggered reel locks (Reel 1 ➔ Reel 2 ➔ Reel 3) gives the player visual excitement.
   - Designed a damped spring settling formula for realistic mechanical heft when reels lock.
3. **Decoupled Architecture**:
   - `PaylineEvaluator` is pure C# logic without Unity dependencies, enabling unit testability.
   - `ReelManager` focuses strictly on the visual/physical presentation of the reels.
   - `SlotMachineController` acts as a coordinator rather than a monolithic god class.
4. **Resilience & WebGL Compatibility**:
   - Provided procedural audio synthesis so that audio operates out-of-the-box regardless of external platform audio codec restrictions.
