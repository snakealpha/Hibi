using System.CodeDom;
using System.Collections.Generic;

namespace Elecelf.Hibiki.Scanner
{
    /// <summary>
    /// Provide helper methods for symol manager in scan phase.
    /// </summary>
    public class SymolHelper
    {
        public SymolHelper()
        {
            _nameToSymolMap["epsilon"] = new Symol("epsilon", 0);
            _nameToSymolMap["eps"] = new Symol("eps", 0);
        }

        private readonly Dictionary<string, Symol> _nameToSymolMap = new Dictionary<string, Symol>();

        private uint _currentAssignedSymolSerial = 1;

        /// <summary>
        /// Get or create a symol with its name.
        /// </summary>
        /// <param name="name">Symol's name</param>
        /// <returns>Symol object</returns>
        public Symol GetSymol(string name)
        {
            lock (this)
            {
                if (!_nameToSymolMap.ContainsKey(name))
                    _nameToSymolMap[name] = new Symol(name, _currentAssignedSymolSerial++);
            }
            return _nameToSymolMap[name];
        }
    }

    public struct Symol
    {

        public Symol(string name, uint serial)
        {
            SymolName = name;
            SymolSerial = serial;
        }

        public string SymolName { get; }

        public uint SymolSerial { get; }

        public override int GetHashCode()
        {
            return (int)SymolSerial;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj.GetHashCode() == GetHashCode();
        }

        public override string ToString()
        {
            return SymolName;
        }

        public static bool operator ==(Symol a, Symol b)
        {
            return a.SymolSerial == b.SymolSerial;
        }

        public static bool operator !=(Symol a, Symol b)
        {
            return a.SymolSerial != b.SymolSerial;
        }

        public static implicit operator uint(Symol a)
        {
            return a.SymolSerial;
        }
    }
}
