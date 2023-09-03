using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace SoulLinker;

public class SoulLinker : BaseSettingsPlugin<SoulLinkerSettings>
{
    internal static SoulLinker Instance;
    private List<Buff> buffs;
    private List<ActorSkill> skills;
    private DateTime nextTickTime;

    public override bool Initialise()
    {
        //Perform one-time initialization here

        //Maybe load you custom config (only do so if builtin settings are inadequate for the job)
        //var configPath = Path.Join(ConfigDirectory, "custom_config.txt");
        //if (File.Exists(configPath))
        //{
        //    var data = File.ReadAllText(configPath);
        //}

        nextTickTime = DateTime.Now;
        Instance = this;

        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
        //Perform once-per-zone processing here
        //For example, Radar builds the zone map texture here
    }

    public override Job Tick()
    {
        //Perform non-render-related work here, e.g. position calculation.
        //This method is still called on every frame, so to really gain
        //an advantage over just throwing everything in the Render method
        //you have to return a custom job, but this is a bit of an advanced technique
        //here's how, just in case:
        //return new Job($"{nameof(SoulLinker)}MainJob", () =>
        //{
        //    var a = Math.Sqrt(7);
        //});

        //otherwise, just run your code here
        //var a = Math.Sqrt(7);

        return new Job($"{nameof(SoulLinker)}MainJob", () =>
        {
            if (!Settings.Enable)
                return;
            if (this.nextTickTime > DateTime.Now)
                return;
            if (GameController.IsLoading || !GameController.InGame || MenuWindow.IsOpened ||
                !GameController.IsForeGroundCache)
                return;
            var player = GameController.Game.IngameState.Data.LocalPlayer;
            if (player == null)
            {
                DebugLogMessage("Player entity not found.");
                return;
            }

            if (!player.IsAlive)
            {
                DebugLogMessage("Player is dead.");
                return;
            }

            var inTown = GameController.Area.CurrentArea.IsTown;
            if (inTown)
            {
                DebugLogMessage("Player is in town.");
                return;
            }

            buffs = player.GetComponent<Buffs>()?.BuffsList;
            if (buffs == null)
            {
                DebugLogMessage("Player buffs list not found.");
                return;
            }

            var gracePeriod = HasBuff("grace_period");
            if (!gracePeriod.HasValue || gracePeriod.Value)
            {
                DebugLogMessage("Player has grace period.");
                return;
            }

            var linkBuff = HasBuff(Settings.linkerBuffName.Value);
            if (!linkBuff.HasValue || linkBuff.Value)
            {
                DebugLogMessage($"Player already has {Settings.linkerBuffName.Value} buff.");
                return;
            }

            skills = player.GetComponent<Actor>().ActorSkills;
            if (skills == null || skills.Count == 0)
            {
                DebugLogMessage("Player skills not found.");
                return;
            }

            var linkSkill = GetUsableSkill(Settings.linkerSkillName.Value);
            if (linkSkill == null)
            {
                DebugLogMessage("Link skill not found or is not usable.");
                return;
            }

            var linkTargetName = Settings.linkerLeaderName.Value;
            if (string.IsNullOrWhiteSpace(linkTargetName))
            {
                DebugLogMessage("Target name is not defined - set it in settings.");
                return;
            }

            var linkTarget = GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Player]
                .FirstOrDefault(p => string.Compare(p.GetComponent<Player>()?.PlayerName, linkTargetName,
                    StringComparison.OrdinalIgnoreCase) == 0);

            if (linkTarget == null)
            {
                DebugLogMessage($"Link target with name {linkTargetName} not found.");
                return;
            }

            var playerPosition = player.Pos;
            var targetPosition = linkTarget.Pos;
            var distance = Vector3.Distance(playerPosition, targetPosition);
            var maxDistance = Settings.linkerMaxDistance.Value;

            if (distance > maxDistance)
            {
                DebugLogMessage($"Distance to target is too far: {distance} > {maxDistance}");
                return;
            }

            Camera camera = GameController.Game.IngameState.Camera;
            if (camera == null)
            {
                DebugLogMessage("Game camera not found.");
                return;
            }

            var targetPosToScreen = camera.WorldToScreen(targetPosition);
            RectangleF vectWindow = GameController.Window.GetWindowRectangle();
            if (targetPosToScreen.Y + 3 > vectWindow.Bottom || targetPosToScreen.Y - 3 < vectWindow.Top)
            {
                LogMessage($"Target {linkTargetName} is not visible on screen.");
                return;
            }

            if (targetPosToScreen.X + 3 > vectWindow.Right || targetPosToScreen.X - 3 < vectWindow.Left)
            {
                LogMessage($"Target {linkTargetName} is not visible on screen.");
                return;
            }

            var windowOffset = GameController.Window.GetWindowRectangle().TopLeft;
            Input.SetCursorPos(targetPosToScreen + windowOffset);
            Input.KeyPress(Settings.linkerSkillKey.Value);

            DebugLogMessage($"Casting skill {Settings.linkerSkillName.Value} on target {linkTargetName}.");

            var tickTime = Settings.tickTime.Value;
            nextTickTime = DateTime.Now + TimeSpan.FromMilliseconds(tickTime);
        });
    }

    private bool? HasBuff(string buffName)
    {
        if (buffs == null)
        {
            LogError("Requested buff check, but buff list is empty.");
            return null;
        }

        return buffs.Any(b => string.Compare(b.Name, buffName, StringComparison.OrdinalIgnoreCase) == 0);
    }

    private ActorSkill GetUsableSkill(string skillName)
    {
        if (skills == null)
        {
            LogError("Requested usable skill, but skill list is empty.");
            return null;
        }

        return skills.FirstOrDefault(s =>
            (string.Compare(s.Name, skillName, StringComparison.OrdinalIgnoreCase) == 0) && s.CanBeUsed);
    }

    private void DebugLogMessage(string message, float time = 1F)
    {
        if (Settings.Debug.Value)
            LogMessage(message, time);
    }

    public override void Render()
    {
        //Any Imgui or Graphics calls go here. This is called after Tick
        //Graphics.DrawText($"Plugin {GetType().Name} is working.", new Vector2(100, 100), Color.Red);
    }

    public override void EntityAdded(Entity entity)
    {
        //If you have a reason to process every entity only once,
        //this is a good place to do so.
        //You may want to use a queue and run the actual
        //processing (if any) inside the Tick method.
    }
}