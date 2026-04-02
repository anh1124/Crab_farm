namespace Script.Utils
{
    public static class GameTags
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string EnemyPortal = "EnemyPortal";
        public const string Wall = "Wall";
        public const string Stump = "Stump";
        public const string Archer = "Archer";
        public const string Villager = "Villager";
        public const string Vagrant = "Vagrant";
        public const string Trigger = "Trigger";
        public const string Coin = "Coin";
        public const string Animal = "Animal";
        public const string Tree = "Tree";
        public const string TownCenter = "TownCenter";
        public const string Denizen = "Denizen";
        public const string Untagged = "Untagged";
        public const string Hammer = "Hammer";
        public const string Bow = "Bow";
    }

    public static class GameLayers
    {
        public const string Ground = "Ground";
        public const string TriggerLayer = "TriggerLayer";
        
        public static int GroundMask => UnityEngine.LayerMask.GetMask(Ground);
        public static int TriggerLayerMask => UnityEngine.LayerMask.GetMask(TriggerLayer);
        
        public static int GroundLayer => UnityEngine.LayerMask.NameToLayer(Ground);
        public static int TriggerLayerIndex => UnityEngine.LayerMask.NameToLayer(TriggerLayer);
    }

    public static class TargetLists
    {
        public static readonly System.Collections.Generic.List<string> ArcherTargets = new System.Collections.Generic.List<string> 
        { 
            GameTags.Enemy, 
            GameTags.EnemyPortal, 
            GameTags.Animal 
        };

        public static readonly System.Collections.Generic.List<string> VagrantTargets = new System.Collections.Generic.List<string> 
        { 
            GameTags.Enemy, 
            GameTags.Coin 
        };

        public static readonly System.Collections.Generic.List<string> VillagerTargets = new System.Collections.Generic.List<string> 
        { 
            GameTags.Enemy, 
            GameTags.Player 
        };

        public static readonly System.Collections.Generic.List<string> EnemyAdvanceTargets = new System.Collections.Generic.List<string> 
        { 
            GameTags.Wall, 
            GameTags.TownCenter, 
            GameTags.Archer, 
            GameTags.Player, 
            GameTags.Denizen,
            GameTags.Villager,
            GameTags.Vagrant
        };

        public static readonly System.Collections.Generic.List<string> EnemyRetreatTargets = new System.Collections.Generic.List<string> 
        { 
            GameTags.Wall 
        };

        public static readonly System.Collections.Generic.List<string> AllyTags = new System.Collections.Generic.List<string> 
        { 
            GameTags.Player, 
            GameTags.Archer, 
            GameTags.Villager,
            GameTags.Vagrant,
            GameTags.Denizen,
            GameTags.Wall, 
            GameTags.TownCenter 
        };

        public static readonly System.Collections.Generic.List<string> FleeTargets = new System.Collections.Generic.List<string> 
        { 
            GameTags.Enemy 
        };

        public static readonly System.Collections.Generic.List<string> ToolTags = new System.Collections.Generic.List<string>
        {
            GameTags.Hammer,
            GameTags.Bow
        };
    }
}
