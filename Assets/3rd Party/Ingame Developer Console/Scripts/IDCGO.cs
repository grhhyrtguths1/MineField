using System.Collections.Generic;

namespace IDC
{
    public class IDCGO
    {
        public string GOName { get; private set; }
        public Dictionary<int, IDCClassInstance> Classes { get; private set; }

        public IDCGO(string goName)
        {
            GOName = goName;
            Classes = new Dictionary<int, IDCClassInstance>();
        }
    }
}