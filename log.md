[CoinUser-DEBUG] SPAWN COIN: Coin(Clone) at (0.00, 0.04, 0.00) to fly to slot 0 at (0.76, 5.00, 0.00). Target collectedCoins 1/1. Player current coin: 49. Transit list count: 1
0x000002596060f993 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:StartCoinTransfer (UnityEngine.GameObject) (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:425)
0x000002596060f15b (Mono JIT Code) Script.Game.Item.Coin.CoinUser:InsertCoin (UnityEngine.GameObject) (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:294)
0x0000025939851fd3 (Mono JIT Code) Script.Controller.PlayerController.PlayerController:UseCoinAction () (at E:/unity/Crab_farm/Assets/Script/Controller/PlayerController/PlayerController.cs:181)
0x0000025939851b6b (Mono JIT Code) Script.Controller.PlayerController.PlayerController:FixedUpdate () (at E:/unity/Crab_farm/Assets/Script/Controller/PlayerController/PlayerController.cs:109)

[CoinUser-DEBUG] ARRIVED COIN: Coin(Clone) arrived at slot 0 with position (0.76, 5.00, 0.00). Player current coin: 49. Arrived array count: 1, Transit array count: 1
0x00000259606107d3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:472)

[CoinUser-DEBUG] CheckUpgradeStatus: collected=1, required=1
0x000002596061239b (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:479)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

[CoinUser-DEBUG] PerformUpgrade CALLED! isReadyToUpgrade=False, collected=1, required=1
0x00000259606124fb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PerformUpgrade () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:321)
0x00000259606123bb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:483)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

[CoinUser-DEBUG] PerformUpgrade executing... set isReadyToUpgrade = True
0x0000025960612743 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PerformUpgrade () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:332)
0x00000259606123bb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:483)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

[CoinUser-DEBUG] ClearTransitCoins called! previous transit count=1
0x00000259606130eb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:ClearTransitCoins () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:526)
0x0000025960612883 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PerformUpgrade () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:350)
0x00000259606123bb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:483)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

[NatureTree-DEBUG] SetChopFlag CALLED on Tree_1(Clone). isReadyToChop was False
0x000002596061324b (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:SetChopFlag () (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:101)
0x00000259606128e0 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PerformUpgrade () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:354)
0x00000259606123bb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:483)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

[NatureTree-DEBUG] TakeDame CALLED on Tree_1(Clone): dame=5, currentHP=0, _isDead=False
0x000002596061347b (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:TakeDame (int) (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:112)
0x000002596061328b (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:SetChopFlag () (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:105)
0x00000259606128e0 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PerformUpgrade () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:354)
0x00000259606123bb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:483)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

[NatureTree-DEBUG] Tree_1(Clone) IS DEAD. Calling Destroy.
0x00000259606134fb (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:TakeDame (int) (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:117)
0x000002596061328b (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:SetChopFlag () (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:105)
0x00000259606128e0 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PerformUpgrade () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:354)
0x00000259606123bb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:483)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

[CoinUser-DEBUG] ClearCoinSlots called! Destroys 1 children and sets currentSlotIndex = 0
0x0000025939855183 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:ClearCoinSlots () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:264)
0x00000259398550a3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PlayerTriggerOff () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:183)
0x0000025939855003 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:OnPlayerExit (Script.Game.Player.PlayerBehavior) (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:545)
0x00000259606115e3 (Mono JIT Code) Script.Game.Player.PlayerInteraction:OnTriggerExit2D (UnityEngine.Collider2D) (at E:/unity/Crab_farm/Assets/Script/Entities/Mobs/Player/PlayerInteraction.cs:69)
0x000002596061353b (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:TakeDame (int) (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:121)
0x000002596061328b (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:SetChopFlag () (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:105)
0x00000259606128e0 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PerformUpgrade () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:354)
0x00000259606123bb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:483)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

[CoinUser-DEBUG] ClearCoinSlots called! Destroys 0 children and sets currentSlotIndex = 0
0x0000025939855183 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:ClearCoinSlots () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:264)
0x0000025939854e6b (Mono JIT Code) Script.Game.Item.Coin.CoinUser:SetupCoinSlots () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:249)
0x0000025939854acb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PlayerTriggerOn () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:178)
0x0000025939854a0b (Mono JIT Code) Script.Game.Item.Coin.CoinUser:OnPlayerEnter (Script.Game.Player.PlayerBehavior) (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:535)
0x000002593985483f (Mono JIT Code) Script.Game.Player.PlayerInteraction:UpdateCurrentInteractable () (at E:/unity/Crab_farm/Assets/Script/Entities/Mobs/Player/PlayerInteraction.cs:118)
0x00000259606115fb (Mono JIT Code) Script.Game.Player.PlayerInteraction:OnTriggerExit2D (UnityEngine.Collider2D) (at E:/unity/Crab_farm/Assets/Script/Entities/Mobs/Player/PlayerInteraction.cs:75)
0x000002596061353b (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:TakeDame (int) (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:121)
0x000002596061328b (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:SetChopFlag () (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:105)
0x00000259606128e0 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PerformUpgrade () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:354)
0x00000259606123bb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:483)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

đây là nhét coin lần 2 
[CoinUser-DEBUG] SPAWN COIN: Coin(Clone) at (0.00, 0.04, 0.00) to fly to slot 0 at (0.76, 5.00, 0.00). Target collectedCoins 1/1. Player current coin: 49. Transit list count: 1
0x000002596060f993 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:StartCoinTransfer (UnityEngine.GameObject) (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:425)
0x000002596060f15b (Mono JIT Code) Script.Game.Item.Coin.CoinUser:InsertCoin (UnityEngine.GameObject) (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:294)
0x0000025939851fd3 (Mono JIT Code) Script.Controller.PlayerController.PlayerController:UseCoinAction () (at E:/unity/Crab_farm/Assets/Script/Controller/PlayerController/PlayerController.cs:181)
0x0000025939851b6b (Mono JIT Code) Script.Controller.PlayerController.PlayerController:FixedUpdate () (at E:/unity/Crab_farm/Assets/Script/Controller/PlayerController/PlayerController.cs:109)

[CoinUser-DEBUG] ARRIVED COIN: Coin(Clone) arrived at slot 0 with position (0.76, 5.00, 0.00). Player current coin: 49. Arrived array count: 1, Transit array count: 1
0x00000259606107d3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:472)

[CoinUser-DEBUG] CheckUpgradeStatus: collected=1, required=1
0x000002596061239b (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:479)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

[CoinUser-DEBUG] PerformUpgrade CALLED! isReadyToUpgrade=False, collected=1, required=1
0x00000259606124fb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PerformUpgrade () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:321)
0x00000259606123bb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:483)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

[CoinUser-DEBUG] PerformUpgrade executing... set isReadyToUpgrade = True
0x0000025960612743 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PerformUpgrade () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:332)
0x00000259606123bb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:483)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

[CoinUser-DEBUG] ClearTransitCoins called! previous transit count=1
0x00000259606130eb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:ClearTransitCoins () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:526)
0x0000025960612883 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PerformUpgrade () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:350)
0x00000259606123bb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:483)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

[NatureTree-DEBUG] SetChopFlag CALLED on Tree_1(Clone). isReadyToChop was False
0x000002596061324b (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:SetChopFlag () (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:101)
0x00000259606128e0 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PerformUpgrade () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:354)
0x00000259606123bb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:483)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

[NatureTree-DEBUG] TakeDame CALLED on Tree_1(Clone): dame=5, currentHP=5, _isDead=False
0x000002596061347b (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:TakeDame (int) (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:112)
0x000002596061328b (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:SetChopFlag () (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:105)
0x00000259606128e0 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PerformUpgrade () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:354)
0x00000259606123bb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:483)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

[NatureTree-DEBUG] Tree_1(Clone) IS DEAD. Calling Destroy.
0x00000259606134fb (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:TakeDame (int) (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:117)
0x000002596061328b (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:SetChopFlag () (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:105)
0x00000259606128e0 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PerformUpgrade () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:354)
0x00000259606123bb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:483)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

[CoinUser-DEBUG] ClearCoinSlots called! Destroys 1 children and sets currentSlotIndex = 0
0x0000025939855183 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:ClearCoinSlots () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:264)
0x00000259398550a3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PlayerTriggerOff () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:183)
0x0000025939855003 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:OnPlayerExit (Script.Game.Player.PlayerBehavior) (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:545)
0x00000259606115e3 (Mono JIT Code) Script.Game.Player.PlayerInteraction:OnTriggerExit2D (UnityEngine.Collider2D) (at E:/unity/Crab_farm/Assets/Script/Entities/Mobs/Player/PlayerInteraction.cs:69)
0x000002596061353b (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:TakeDame (int) (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:121)
0x000002596061328b (Mono JIT Code) Script.Game.Other.Nature.NatureTreeBehavior:SetChopFlag () (at E:/unity/Crab_farm/Assets/Script/Entities/Nature/NatureTree/NatureTreeBehavior.cs:105)
0x00000259606128e0 (Mono JIT Code) Script.Game.Item.Coin.CoinUser:PerformUpgrade () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:354)
0x00000259606123bb (Mono JIT Code) Script.Game.Item.Coin.CoinUser:CheckUpgradeStatus () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:483)
0x00000259606107f3 (Mono JIT Code) Script.Game.Item.Coin.CoinUser/<MoveCoinToSlot>d__44:MoveNext () (at E:/unity/Crab_farm/Assets/Script/Entities/Item/Coin/CoinUser.cs:474)

Call to DeinitializeLoader without an initialized manager.Please make sure to wait for initialization to complete before calling this API.

[PlayerBehavior] Unregistered from PlayerController.
0x00000259606272a3 (Mono JIT Code) Script.Game.Player.PlayerBehavior:OnDestroy () (at E:/unity/Crab_farm/Assets/Script/Entities/Mobs/Player/PlayerBehavior.cs:138)

