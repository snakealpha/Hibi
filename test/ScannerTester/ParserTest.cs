using System;
using Elecelf.Hibiki.Scanner;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ScannerTester
{
    [TestClass]
    public class ParserTest
    {
        private readonly ScannerContext UsecaseContext = new ScannerContext();

        [TestMethod]
        public void SimpleElement()
        {
            // Simple string element
            var rawString = @"string";
            var grammar = GrammarAutomata.Parse(rawString, UsecaseContext);
            Assert.IsTrue(
                grammar.StartState.IsTerminal != true &&                                                                            // Start state should not be a terminal state.
                grammar.StartState.Transfers.Count == 1 &&                                                                          // Only one transfer in start state.
                grammar.StartState.Transfers[0].TransferCondition is StringTransferCondition &&                                     // Type of the transfer is a string transfer.
                (grammar.StartState.Transfers[0].TransferCondition as StringTransferCondition).CompareReference == "string" &&      // Liter of transfer should be rawstring.
                grammar.StartState.Transfers[0].TransfedState.IsTerminal);                                                          // this only transfer leads to the only terminal state.
        }
    }
}
