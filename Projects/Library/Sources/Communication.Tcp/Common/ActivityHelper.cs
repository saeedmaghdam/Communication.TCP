using System.Diagnostics;
using System.Linq;

namespace Mabna.Communication.Tcp.Common
{
    public static class ActivityHelper
    {
        public static Activity Start()
        {
            if (Activity.Current != null)
                return Activity.Current;

            var st = new StackTrace(true);
            var sf = st.GetFrame(1);

            var parameterString = "";
            var parametersList = sf.GetMethod().GetParameters();
            if (parametersList.Length > 0)
            {
                parameterString = "(";
                parameterString += string.Join(", ", parametersList.Select(x => x.Name));
                parameterString += ")";
            }

            var activity = new Activity($"{sf.GetMethod().ReflectedType.FullName}.{sf.GetMethod().Name}{parameterString}");
            activity.Start();

            return activity;
        }
    }
}
