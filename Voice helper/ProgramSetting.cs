using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voice_helper
{
    public class ProgramSetting
    {
        public string aliasName;
        public string argument;
        public string actionValue;

        public ProgramSetting(string _aliasName, string _argument, string _actionValue)
        {
            aliasName = _aliasName;
            argument = _argument;
            actionValue = _actionValue;
        }
    }
}