﻿using System.Collections.Generic;
using IQToolkitCodeGen.Model;
using Microsoft.Practices.Prism.Events;

namespace IQToolkitCodeGen.Event
{
    public class MostRecentlyUsedFileChangedEvent : CompositePresentationEvent<IEnumerable<MostRecentlyUsed>>
    {
    }
}

