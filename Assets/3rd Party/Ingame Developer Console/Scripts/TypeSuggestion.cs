using System;

namespace IDC
{
    public struct TypeSuggestion
    {
        public string[] suggs;
        public Func<string[]> suggsFunc;

        public TypeSuggestion(string[] suggs, Func<string[]> suggsFunc)
        {
            this.suggs = suggs;
            this.suggsFunc = suggsFunc ?? new Func<string[]>(() => { return new string[0]; });
        }
    }
}