using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Talking_ben_bot
{
    public class Chance
    {
        private readonly static Random _random = new();
        public static async Task Proc(List<KeyValuePair<int, Func<Task>>> chances)
        {
            var sorted = chances.OrderByDescending(x => x.Key);

            Func<Task> procFunc = null;
            while (procFunc == null)
            {
                foreach (var (chance, func) in sorted)
                {
                    var num = _random.Next(0, 100);
                    if (num <= chance)
                    {
                        procFunc = func;

                        break;
                    }
                }
            }

            await procFunc.Invoke();
        }
    }
}
