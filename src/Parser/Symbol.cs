using System;
using System.Collections.Generic;

namespace Elecelf.Hibiki.Parser
{
    /// <summary>
    /// Provide helper methods for symbol manager in scan phase.
    /// </summary>
    public class SymbolHelper
    {
        public SymbolHelper()
        {
            _nameToSymbolMap["epsilon"] = new Symbol("epsilon", 0, null);
            _nameToSymbolMap["eps"] = new Symbol("eps", 0, null);
        }

        private readonly Dictionary<string, Symbol> _nameToSymbolMap = new Dictionary<string, Symbol>();

        private uint _currentAssignedSymolSerial = 1;

        /// <summary>
        /// Get or create a symbol with its name.
        /// </summary>
        /// <param name="name">Symbol's name</param>
        /// <returns>Symbol object</returns>
        public Symbol GetSymol(string name)
        {
            lock (this)
            {
                if (!_nameToSymbolMap.ContainsKey(name))
                    _nameToSymbolMap[name] = new Symbol(name, _currentAssignedSymolSerial++, this);
            }
            return _nameToSymbolMap[name];
        }

        public bool InsertSymbol(string name, uint symbolId)
        {
            lock (this)
            {
                if (_nameToSymbolMap.ContainsKey(name))
                    return false;

                _nameToSymbolMap[name] = new Symbol(name, symbolId, this);
                _currentAssignedSymolSerial = Math.Max(_currentAssignedSymolSerial, symbolId);
                return true;
            }
        }
    }

    public struct Symbol
    {
        public Symbol(string name, uint serial, object host)
        {
            SymbolName = name;
            SymbolSerial = serial;
            SymbolHost = host;
        }

        public string SymbolName { get; }

        public uint SymbolSerial { get; }

        public object SymbolHost { get; }

        public override int GetHashCode()
        {
            return (int)SymbolSerial;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj.GetHashCode() == GetHashCode();
        }

        public override string ToString()
        {
            return SymbolName;
        }

        public static bool operator ==(Symbol a, Symbol b)
        {
            return a.SymbolHost == b.SymbolHost && a.SymbolSerial == b.SymbolSerial;
        }

        public static bool operator !=(Symbol a, Symbol b)
        {
            return a.SymbolHost != b.SymbolHost || a.SymbolSerial != b.SymbolSerial;
        }

        public static implicit operator uint(Symbol a)
        {
            return a.SymbolSerial;
        }
    }
}
