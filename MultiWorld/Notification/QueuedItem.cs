using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MultiWorld;
public class QueuedItem(string itemId, int index, string player)
{
    public string itemId = itemId;
    public int index = index;
    public string player = player;
}