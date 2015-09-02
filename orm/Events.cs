using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace zhichkin
{
    namespace orm
    {
        public class StateEventArgs : EventArgs
        {
            private PersistenceState old_state;
            private PersistenceState new_state;
            
            private StateEventArgs() { }

            public StateEventArgs(PersistenceState old_state, PersistenceState new_state)
            {
                this.old_state = old_state;
                this.new_state = new_state;
            }

            public PersistenceState OldState { get { return old_state; } }
            public PersistenceState NewState { get { return new_state; } }
        }
        public delegate void StateChangingEventHandler(IPersistent sender, StateEventArgs args);
        public delegate void StateChangedEventHandler(IPersistent sender, StateEventArgs args);

        public delegate void SaveEventHandler(IPersistent entity);
        public delegate void KillEventHandler(IPersistent entity);
        public delegate void LoadEventHandler(IPersistent entity);
    }
}
