# ⚔️ Ship Battle Game - Project Context

## 📋 Project Overview

**Game Name:** Ship Battle (Tàu Chiến)  
**Engine:** Unity  
**Networking:** Photon Fusion 2 (Real-time Multiplayer)  
**Target:** .NET Framework 4.7.1, C# 9.0  
**Platforms:** PC + Mobile (iOS/Android)  
**Genre:** Action Multiplayer 2D Pixel Art

---

## 🎮 Core Game Mechanics

### Player Actions
- **Movement:** 8-direction movement + rotation toward mouse/joystick
- **Shooting:** Single-shot bullets with 20 ammo magazine
- **Reload:** 3-second reload time
- **Dash:** 1.5-second cooldown, applies invincibility during dash
- **Health:** 100 HP base health with visual health bar

### Multiplayer Features
- **Player Sync:** Real-time position, rotation, sprite, ammo sync via Photon Fusion
- **Win Condition:** Last player alive wins (active player tracking)
- **Spectator Mode:** Eliminated players spectate remaining players
- **Max Players:** 4-8 per session

### Power-ups (Mystery Box)
- **Stun Effect:** Paralyzes enemy for 2 seconds
- **Inverted Controls:** Reverses movement input for 3 seconds
- **Bullet Modification:** Random bullet types (bouncing, big bullets, etc.)

---

## 📁 Project Structure

```
Assets/_Scripts/
├── Core Gameplay
│   ├── PlayerController.cs         [Movement, Shooting, Dash]
│   ├── HealthComponent.cs          [Health + Damage System]
│   ├── BulletController.cs         [Bullet Physics & Hit Detection]
│   ├── BaseBullet.cs               [Bullet Base Class]
│   ├── BouncingBullet.cs           [Special Bullet Type]
│   └── Big Bullet.cs               [Special Bullet Type]
│
├── Spawning & Effects
│   ├── BasicSpawner.cs             [Player Spawn Points]
│   ├── DeathExplosionSpawner.cs    [Death VFX]
│   ├── MysteryBoxSpawner.cs        [Power-up Spawn System]
│   ├── MysteryBox.cs               [Power-up Pickup Logic]
│   ├── MysteryEffectData.cs        [Power-up Data]
│   └── ExplosionSimple.cs          [Particle Effects]
│
├── Networking
│   ├── NetworkInputData.cs         [Input Struct for Sync]
│   ├── NetworkChecker.cs           [Network Verification (2-level)]
│   └── SmoothVisual.cs             [Smooth Movement Interpolation]
│
├── Input & Controls
│   ├── MobileInputManager.cs       [Touch Input Detection]
│   ├── ProDPad.cs                  [Virtual D-Pad (Mobile)]
│   ├── DPadButton.cs               [D-Pad Button Component]
│   └── ActionButton.cs             [Action Button Component]
│
├── UI Management
│   ├── LocalUI.cs                  [HUD - Health/Ammo/Dash]
│   ├── GameplayUIManager.cs        [Gameplay UI Orchestration]
│   ├── UIFixedRotation.cs          [UI Always Faces Camera]
│   ├── EnemyHealthBar.cs           [Enemy Health Display]
│   └── Setting/
│       ├── SettingsPanelUI.cs      [Settings Panel (Pause)]
│       └── FPSCounter.cs           [FPS Display Toggle]
│
├── Audio System
│   ├── AudioManager.cs             [Singleton Audio Manager]
│   └── SpatialAudioManager.cs      [3D Spatial Audio]
│
├── Startup & Menu
│   ├── AppSetup.cs                 [Game Initialization]
│   ├── AppManager.cs               [Main Menu Manager]
│   ├── RoomListEntry.cs            [Room List UI Entry]
│   └── LobbyPlayer.cs              [Lobby Player Display]
```

---

## 🔌 Key Systems

### Audio System (Singleton)
```
AudioManager Methods:
- PlayMusic(AudioClip clip)          [Background music with loop control]
- PlayShoot()                         [Shoot SFX]
- PlayDash()                          [Dash SFX]
- SetMusicVolume(float vol)           [Range 0-1, default 0.6]
- SetSFXVolume(float vol)             [Range 0-1, default 0.8]
- LoadAudioSettings()                 [Load from PlayerPrefs]

Persistence:
- "Audio_Music"      → float (0-1)
- "Audio_SFX"        → float (0-1)
- "Settings_ShowFPS" → int (0/1)
```

### Networking Flow
```
Player Input → NetworkInputData (Struct) → Photon Fusion RPC/OnInput
OnInput() → ReadNetworkInput() → Process movement/shooting
State Changes → OnChanged callbacks → UI updates
```

### Network Checks (2-Level)
```
Level 1: Application.internetReachability check (fast)
Level 2: Ping URL verification (timeout: 5s)
→ If both pass: Play background music, start gameplay
→ If fails: Show error popup, allow exit
```

### Input Priority
```
Desktop:        Mouse position → Movement input via keyboard/WASD
Mobile:         ProDPad joystick → Touch input → Movement input
Platform Detection: MobileInputManager.IsMobilePlatform()
```

---

## ⚙️ Important Properties & Methods

### PlayerController.cs
```csharp
[Networked] public float networkRotation { get; set; }
[Networked] public int currentAmmo { get; set; }
[Networked] public byte currentSpriteIndex { get; set; }
[Networked] public int currentHealth { get; set; }
[Networked] public bool isDashing { get; set; }
[Networked] public byte stunStatus { get; set; }           // 0=none, 1=inverted, 2=stunned
[Networked] public float dashCooldownTimer { get; set; }   // Local only

Methods:
- ProcessDashLogic()              [Dash input handling]
- OnInput(NetworkInput input)     [Photon input callback]
- ReadNetworkInput()              [Parse input struct]
- RPC_PlayShootSound()            [Network audio sync]
```

### HealthComponent.cs
```csharp
[Networked] public int currentHealth { get; set; }

Events:
- OnHealthChanged    [Fired on damage taken]
- OnHealthDepleted   [Fired on death]
```

### GameplayUIManager.cs
```csharp
Methods:
- UpdateHealthUI(int health)           [Sync health bar]
- UpdateAmmoUI(int ammo)               [Sync ammo counter]
- UpdateDashUI(float cooldownPercent)  [Sync dash cooldown bar]
- ShowWinPanel(string playerName)      [Show victory screen]
- SwapMobileUI(bool isMobile)          [Toggle mobile controls visibility]
```

---

## 🔐 Important Guards & Patterns

### Authority Checks
```csharp
if (Object.HasStateAuthority)      // Can modify networked state
if (Object.HasInputAuthority)      // Can read input
if (Runner.IsServer)               // Server-only logic
```

### Memory Management
- `OnHealthChangedEvent`: Always unsubscribe in OnDestroy
- `Singleton managers`: Use lazy initialization pattern
- `Network objects`: Auto-cleanup via Photon Fusion

### UI Patterns
- `SetValueWithoutNotify()` for sliders (prevents callback loops)
- `Time.timeScale = 0` for pause menu
- `Canvas world space` for player-mounted UI (health bars, names)

---

## 📊 Game Balance Values

| Parameter | Value | Location |
|-----------|-------|----------|
| Base Health | 100 HP | PlayerController.cs |
| Max Ammo | 20 rounds | PlayerController.cs |
| Reload Time | 3.0s | PlayerController.cs |
| Dash Cooldown | 1.5s | PlayerController.cs |
| Stun Duration | 2.0s | MysteryBox.cs |
| Inverted Duration | 3.0s | MysteryBox.cs |
| Music Volume Default | 0.6 (60%) | AudioManager.cs |
| SFX Volume Default | 0.8 (80%) | AudioManager.cs |
| Network Timeout | 5s | NetworkChecker.cs |

---

## ✅ Completed Features (v1.7)

- ✅ Core gameplay (movement, shooting, dash, health)
- ✅ Photon Fusion 2 networking + player sync
- ✅ Multiplayer room/lobby system
- ✅ Mobile controls (touch + D-pad)
- ✅ Power-up system (mystery box with effects)
- ✅ Audio system with volume control
- ✅ Settings panel with persistence (PlayerPrefs)
- ✅ FPS counter with toggle
- ✅ Network checker (2-level verification)
- ✅ Background music on startup

---

## 🚀 Development Guidelines

### When Adding New Features
1. Check existing networked properties in `PlayerController.cs`
2. Use `[Networked]` attribute for sync properties
3. Call `RPC_*` methods for non-deterministic actions (audio, VFX)
4. Test on both desktop and mobile platforms
5. Update `DEV_LOG.md` with progress

### Common Debugging
```csharp
// Network debugging
if (HasInputAuthority) Debug.Log($"Input: {input}");

// UI debugging
Debug.Log($"Health: {currentHealth}, Ammo: {currentAmmo}");

// Audio debugging
AudioManager.Instance?.PlayShoot(); // Safe null check
```

### Adding Audio
```csharp
// Via Singleton
AudioManager.Instance.PlayShoot();
AudioManager.Instance.SetMusicVolume(0.5f);

// Via spatial audio for networked players
GetComponent<SpatialAudioManager>()?.PlayShootAt(transform);
```

---

## 📌 Quick Reference

**Main Scene Entry Points:**
- `AppSetup.cs` → Game initialization (startup music)
- `BasicSpawner.cs` → Player spawn on connect
- `AppManager.cs` → Main menu (room creation/join)

**Network Entry Points:**
- `PlayerController.OnInput()` → Input processing
- `PlayerController.OnChanged()` → State sync
- `RPC_PlayShootSound()` → Audio sync

**UI Entry Points:**
- `LocalUI.cs` → HUD updates
- `GameplayUIManager.cs` → Global gameplay UI
- `SettingsPanelUI.cs` → Settings menu

---

**Last Updated:** v1.7 - Network Check & Startup Music Complete  
**Status:** 🟢 Ready for deployment & testing
