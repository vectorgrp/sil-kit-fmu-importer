﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.42000
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------

using System.Xml.Serialization;

// 
// This source code was auto-generated by xsd, Version=4.8.3928.0.
// 


/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlRootAttribute("Annotations", Namespace="", IsNullable=false)]
public partial class fmi3Annotations {
    
    private fmi3AnnotationsAnnotation[] annotationField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("Annotation")]
    public fmi3AnnotationsAnnotation[] Annotation {
        get {
            return this.annotationField;
        }
        set {
            this.annotationField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
public partial class fmi3AnnotationsAnnotation {
    
    private System.Xml.XmlNode[] anyField;
    
    private string typeField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlTextAttribute()]
    [System.Xml.Serialization.XmlAnyElementAttribute()]
    public System.Xml.XmlNode[] Any {
        get {
            return this.anyField;
        }
        set {
            this.anyField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="normalizedString")]
    public string type {
        get {
            return this.typeField;
        }
        set {
            this.typeField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
public partial class fmi3Terminal {
    
    private fmi3TerminalTerminalMemberVariable[] terminalMemberVariableField;
    
    private fmi3TerminalTerminalStreamMemberVariable[] terminalStreamMemberVariableField;
    
    private fmi3Terminal[] terminalField;
    
    private fmi3TerminalTerminalGraphicalRepresentation terminalGraphicalRepresentationField;
    
    private fmi3Annotations annotationsField;
    
    private string nameField;
    
    private string matchingRuleField;
    
    private string terminalKindField;
    
    private string descriptionField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("TerminalMemberVariable")]
    public fmi3TerminalTerminalMemberVariable[] TerminalMemberVariable {
        get {
            return this.terminalMemberVariableField;
        }
        set {
            this.terminalMemberVariableField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("TerminalStreamMemberVariable")]
    public fmi3TerminalTerminalStreamMemberVariable[] TerminalStreamMemberVariable {
        get {
            return this.terminalStreamMemberVariableField;
        }
        set {
            this.terminalStreamMemberVariableField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("Terminal")]
    public fmi3Terminal[] Terminal {
        get {
            return this.terminalField;
        }
        set {
            this.terminalField = value;
        }
    }
    
    /// <remarks/>
    public fmi3TerminalTerminalGraphicalRepresentation TerminalGraphicalRepresentation {
        get {
            return this.terminalGraphicalRepresentationField;
        }
        set {
            this.terminalGraphicalRepresentationField = value;
        }
    }
    
    /// <remarks/>
    public fmi3Annotations Annotations {
        get {
            return this.annotationsField;
        }
        set {
            this.annotationsField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="normalizedString")]
    public string name {
        get {
            return this.nameField;
        }
        set {
            this.nameField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="normalizedString")]
    public string matchingRule {
        get {
            return this.matchingRuleField;
        }
        set {
            this.matchingRuleField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="normalizedString")]
    public string terminalKind {
        get {
            return this.terminalKindField;
        }
        set {
            this.terminalKindField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string description {
        get {
            return this.descriptionField;
        }
        set {
            this.descriptionField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
public partial class fmi3TerminalTerminalMemberVariable {
    
    private fmi3Annotations annotationsField;
    
    private string variableNameField;
    
    private string memberNameField;
    
    private string variableKindField;
    
    /// <remarks/>
    public fmi3Annotations Annotations {
        get {
            return this.annotationsField;
        }
        set {
            this.annotationsField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="normalizedString")]
    public string variableName {
        get {
            return this.variableNameField;
        }
        set {
            this.variableNameField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="normalizedString")]
    public string memberName {
        get {
            return this.memberNameField;
        }
        set {
            this.memberNameField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="normalizedString")]
    public string variableKind {
        get {
            return this.variableKindField;
        }
        set {
            this.variableKindField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
public partial class fmi3TerminalTerminalStreamMemberVariable {
    
    private fmi3Annotations annotationsField;
    
    private string inStreamMemberNameField;
    
    private string outStreamMemberNameField;
    
    private string inStreamVariableNameField;
    
    private string outStreamVariableNameField;
    
    /// <remarks/>
    public fmi3Annotations Annotations {
        get {
            return this.annotationsField;
        }
        set {
            this.annotationsField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="normalizedString")]
    public string inStreamMemberName {
        get {
            return this.inStreamMemberNameField;
        }
        set {
            this.inStreamMemberNameField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="normalizedString")]
    public string outStreamMemberName {
        get {
            return this.outStreamMemberNameField;
        }
        set {
            this.outStreamMemberNameField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="normalizedString")]
    public string inStreamVariableName {
        get {
            return this.inStreamVariableNameField;
        }
        set {
            this.inStreamVariableNameField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="normalizedString")]
    public string outStreamVariableName {
        get {
            return this.outStreamVariableNameField;
        }
        set {
            this.outStreamVariableNameField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
public partial class fmi3TerminalTerminalGraphicalRepresentation {
    
    private fmi3Annotations annotationsField;
    
    private byte[] defaultConnectionColorField;
    
    private double defaultConnectionStrokeSizeField;
    
    private bool defaultConnectionStrokeSizeFieldSpecified;
    
    private double x1Field;
    
    private double y1Field;
    
    private double x2Field;
    
    private double y2Field;
    
    private string iconBaseNameField;
    
    /// <remarks/>
    public fmi3Annotations Annotations {
        get {
            return this.annotationsField;
        }
        set {
            this.annotationsField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public byte[] defaultConnectionColor {
        get {
            return this.defaultConnectionColorField;
        }
        set {
            this.defaultConnectionColorField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double defaultConnectionStrokeSize {
        get {
            return this.defaultConnectionStrokeSizeField;
        }
        set {
            this.defaultConnectionStrokeSizeField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public bool defaultConnectionStrokeSizeSpecified {
        get {
            return this.defaultConnectionStrokeSizeFieldSpecified;
        }
        set {
            this.defaultConnectionStrokeSizeFieldSpecified = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double x1 {
        get {
            return this.x1Field;
        }
        set {
            this.x1Field = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double y1 {
        get {
            return this.y1Field;
        }
        set {
            this.y1Field = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double x2 {
        get {
            return this.x2Field;
        }
        set {
            this.x2Field = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double y2 {
        get {
            return this.y2Field;
        }
        set {
            this.y2Field = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string iconBaseName {
        get {
            return this.iconBaseNameField;
        }
        set {
            this.iconBaseNameField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
public partial class fmiTerminalsAndIcons {
    
    private fmiTerminalsAndIconsGraphicalRepresentation graphicalRepresentationField;
    
    private fmi3Terminal[] terminalsField;
    
    private fmi3Annotations annotationsField;
    
    private string fmiVersionField;
    
    /// <remarks/>
    public fmiTerminalsAndIconsGraphicalRepresentation GraphicalRepresentation {
        get {
            return this.graphicalRepresentationField;
        }
        set {
            this.graphicalRepresentationField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlArrayItemAttribute("Terminal", IsNullable=false)]
    public fmi3Terminal[] Terminals {
        get {
            return this.terminalsField;
        }
        set {
            this.terminalsField = value;
        }
    }
    
    /// <remarks/>
    public fmi3Annotations Annotations {
        get {
            return this.annotationsField;
        }
        set {
            this.annotationsField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="normalizedString")]
    public string fmiVersion {
        get {
            return this.fmiVersionField;
        }
        set {
            this.fmiVersionField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
public partial class fmiTerminalsAndIconsGraphicalRepresentation {
    
    private fmiTerminalsAndIconsGraphicalRepresentationCoordinateSystem coordinateSystemField;
    
    private fmiTerminalsAndIconsGraphicalRepresentationIcon iconField;
    
    private fmi3Annotations annotationsField;
    
    /// <remarks/>
    public fmiTerminalsAndIconsGraphicalRepresentationCoordinateSystem CoordinateSystem {
        get {
            return this.coordinateSystemField;
        }
        set {
            this.coordinateSystemField = value;
        }
    }
    
    /// <remarks/>
    public fmiTerminalsAndIconsGraphicalRepresentationIcon Icon {
        get {
            return this.iconField;
        }
        set {
            this.iconField = value;
        }
    }
    
    /// <remarks/>
    public fmi3Annotations Annotations {
        get {
            return this.annotationsField;
        }
        set {
            this.annotationsField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
public partial class fmiTerminalsAndIconsGraphicalRepresentationCoordinateSystem {
    
    private double x1Field;
    
    private double y1Field;
    
    private double x2Field;
    
    private double y2Field;
    
    private double suggestedScalingFactorTo_mmField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double x1 {
        get {
            return this.x1Field;
        }
        set {
            this.x1Field = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double y1 {
        get {
            return this.y1Field;
        }
        set {
            this.y1Field = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double x2 {
        get {
            return this.x2Field;
        }
        set {
            this.x2Field = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double y2 {
        get {
            return this.y2Field;
        }
        set {
            this.y2Field = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double suggestedScalingFactorTo_mm {
        get {
            return this.suggestedScalingFactorTo_mmField;
        }
        set {
            this.suggestedScalingFactorTo_mmField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
public partial class fmiTerminalsAndIconsGraphicalRepresentationIcon {
    
    private double x1Field;
    
    private double y1Field;
    
    private double x2Field;
    
    private double y2Field;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double x1 {
        get {
            return this.x1Field;
        }
        set {
            this.x1Field = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double y1 {
        get {
            return this.y1Field;
        }
        set {
            this.y1Field = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double x2 {
        get {
            return this.x2Field;
        }
        set {
            this.x2Field = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public double y2 {
        get {
            return this.y2Field;
        }
        set {
            this.y2Field = value;
        }
    }
}
