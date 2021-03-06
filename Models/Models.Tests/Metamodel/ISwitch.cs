//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

using NMF.Collections.Generic;
using NMF.Collections.ObjectModel;
using NMF.Expressions;
using NMF.Expressions.Linq;
using NMF.Models;
using NMF.Models.Collections;
using NMF.Models.Expressions;
using NMF.Models.Meta;
using NMF.Serialization;
using NMF.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace NMF.Models.Tests.Railway
{
    
    
    /// <summary>
    /// The public interface for Switch
    /// </summary>
    [DefaultImplementationTypeAttribute(typeof(Switch))]
    [XmlDefaultImplementationTypeAttribute(typeof(Switch))]
    public interface ISwitch : IModelElement, ITrackElement
    {
        
        /// <summary>
        /// The currentPosition property
        /// </summary>
        Position CurrentPosition
        {
            get;
            set;
        }
        
        /// <summary>
        /// The positions property
        /// </summary>
        IListExpression<ISwitchPosition> Positions
        {
            get;
        }
        
        /// <summary>
        /// Gets fired before the CurrentPosition property changes its value
        /// </summary>
        event EventHandler CurrentPositionChanging;
        
        /// <summary>
        /// Gets fired when the CurrentPosition property changed its value
        /// </summary>
        event EventHandler<ValueChangedEventArgs> CurrentPositionChanged;
    }
}

