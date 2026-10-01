using System;
using System.Collections.Generic;
using Ossuary.Core.Entities;
using Ossuary.Core.Items;
using Ossuary.Core.World;

namespace Ossuary.Core
{
    /// <summary>
    /// Input-driven flow. The UI layer raises UI requests and this class resolves
    /// them, so Commands never has to know about panels and the renderer never has
    /// to know about commands.
    /// </summary>
    public sealed class GameHud
    {
        readonly Game _g;
        readonly Ui _ui;
        readonly Commands _cmd;

        public GameHud(Game g)
        {
            _g = g;
            _ui = new Ui(g);
            _cmd = new Commands(g);
        }

        public Ui Ui => _ui;
        public Commands Cmd => _cmd;
        public Game Game => _g;

        public TextBuilder Draw() => _ui.Draw();
    }
}