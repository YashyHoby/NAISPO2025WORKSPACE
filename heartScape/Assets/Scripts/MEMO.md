Assets/
└─ Scripts/
   ├─ Core/
   │  ├─ Agents/
   │  │  ├─ HeartAgent.cs
   │  │  ├─ HeartManager.cs
   │  │  └─ HeartGrowth.cs
   │  ├─ Appearance/
   │  │  ├─ HeartVisual.cs
   │  │  └─ HeartAppearanceMapper.cs
   │  └─ Systems/
   │     └─ EventApplier.cs
   ├─ Physics/
   │  └─ HeartPhysics.cs
   ├─ Flow/
   │  ├─ FlowManager2D.cs
   │  ├─ IFlowSource2D.cs
   │  ├─ FlowRectSource2D.cs   ← 現在のファイル名は FlowReceSource2D.cs（リネーム推奨）
   │  └─ FlowWall2D.cs
   ├─ Interaction/
   │  └─ Zones/
   │     ├─ EventZone.cs
   │     ├─ FoodZone.cs
   │     ├─ BubbleZone.cs
   │     └─ SoundObstacle.cs
   ├─ IO/
   │  ├─ Gateway/
   │  │  └─ HeartGateway.cs
   │  └─ DB/
   │     └─ HeartDB.cs         ← 現在のファイル名は HaartDB.cs（リネーム推奨）
   └─ Debug/
      └─ DebugSpawner.cs

Assets/Resources/
└─ heart_db.json
