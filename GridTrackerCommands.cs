using ALE_Core.Utils;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;
using VRage.Groups;
using VRageMath;

namespace GridTrackerPlugin
{
    public class GridTrackerCommands : CommandModule
    {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public GridTrackerPlugin Plugin => (GridTrackerPlugin)Context.Plugin;

        [Command("trackgrid", "Track the grid you are looking at, by adding a GPS to its location")]
        [Permission(MyPromoteLevel.SpaceMaster)]
        public void Track()
        {
            IMyPlayer player = Context.Player;

            if (player == null)
            {

                Context.Respond("Console has no Character so cannot use this command. Use !protect <gridname> instead!");
                return;
            }

            IMyCharacter character = player.Character;
            if (character == null)
            {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            try
            {
                var groups = GridFinder.FindLookAtGridGroupMechanical(Context.Player?.Character);

                if (!CheckGroups(groups, out MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group group, Context))
                    return;

                foreach (MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Node groupNodes in group.Nodes)
                {
                    MyCubeGrid grid = groupNodes.NodeData;

                    MyGps gps = new MyGps
                    {
                        Coords = grid.PositionComp.GetPosition(),
                        Name = string.Format("{0}", grid.DisplayName),
                        ShowOnHud = true,
                        GPSColor = Color.Orange,
                        DiscardAt = new TimeSpan?()
                    };
                    gps.SetEntity(grid);
                    gps.UpdateHash();

                    Context.Torch.CurrentSession.KeenSession.Gpss.SendAddGps(player.Identity.IdentityId, ref gps, grid.EntityId, false);
                }

                Context.Respond("Grid is getting tracked!");

            }
            catch (Exception e)
            {
                Log.Error(e, "Error trying to track the grid");
            }

        }

        public static bool CheckGroups(ConcurrentBag<MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group> groups,
                out MyGroups<MyCubeGrid, MyGridMechanicalGroupData>.Group group, CommandContext Context)
        {

            /* No group or too many groups found */
            if (groups.Count < 1)
            {

                Context.Respond("Could not find the Grid.");
                group = null;

                return false;
            }

            /* too many groups found */
            if (groups.Count > 1)
            {

                Context.Respond("Found multiple Grids with same Name. Make sure the name is unique.");
                group = null;

                return false;
            }

            if (!groups.TryPeek(out group))
            {
                Context.Respond("Could not work with found grid for unknown reason.");
                return false;
            }

            return true;
        }

    }
}
