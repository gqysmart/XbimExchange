﻿using System;
using System.Linq;
using Xbim.COBie.Rows;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Common;
using Xbim.Common.Metadata;
using Xbim.Ifc2x3.MaterialResource;

namespace Xbim.COBie.Serialisers.XbimSerialiser
{
    public class COBieXBimType : COBieXBim
    {
        public COBieXBimType(COBieXBimContext xBimContext)
            : base(xBimContext)
        {
           
        }

        #region Methods
        /// <summary>
        /// Create and setup objects held in the Type COBieSheet
        /// </summary>
        /// <param name="cOBieSheet">COBieSheet of COBieTypeRow to read data from</param>
        public void SerialiseType(COBieSheet<COBieTypeRow> cOBieSheet)
        {
            using (var trans = Model.BeginTransaction("Add Type"))
            {
                try
                {
                    var count = 1;
                    ProgressIndicator.ReportMessage("Starting Types...");
                    ProgressIndicator.Initialise("Creating Types", cOBieSheet.RowCount);
                    for (var i = 0; i < cOBieSheet.RowCount; i++)
                    {
                        BumpTransaction(trans, count);
                        count++;
                        ProgressIndicator.IncrementAndUpdate();
                        var row = cOBieSheet[i];
                        if ((ValidateString(row.ExtObject)) &&
                            (row.ExtObject.ToLower().Contains("ifcmaterial"))
                            )
                            AddMaterial(row);
                        else
                            AddType(row);
                    }

                    ProgressIndicator.Finalise();
                    trans.Commit();
                }
                catch (Exception)
                {
                    //TODO: Catch with logger?
                    throw;
                }
            }
        }

        /// <summary>
        /// Add the data to the Material object
        /// </summary>
        /// <param name="row">COBieTypeRow holding the data</param>
        
        private void AddMaterial(COBieTypeRow row)
        {
            
            if (ValidateString(row.Name))
            {
                IfcMaterial ifcMaterial = null;
                //we will skip over the IfcMaterialLayerSets and allow the assembly sheet to create them
                if ((row.ExtObject.ToLower() == "ifcmaterial") ||
                    (row.ExtObject.ToLower() == "ifcmateriallayer")
                    )
                {
                    var name = GetMaterialName(row.Name);
                    ifcMaterial = Model.FederatedInstances.Where<IfcMaterial>(m => m.Name.ToString().ToLower() == name.ToLower()).FirstOrDefault();
                    if (ifcMaterial == null)
                        ifcMaterial = Model.Instances.New<IfcMaterial>(m => { m.Name = name; });
                }
                if ((ifcMaterial != null) && (row.ExtObject.ToLower() == "ifcmateriallayer"))
                {
                    IfcMaterialLayer ifcMaterialLayer = null;
                    var matThick = 0.0;
                    if ((ValidateString(row.NominalWidth)) &&
                        (!double.TryParse(row.NominalWidth, out matThick))
                        )
                        matThick = 0.0;
                    ifcMaterialLayer = Model.FederatedInstances.Where<IfcMaterialLayer>(ml => ml.Material == ifcMaterial && ml.LayerThickness == matThick).FirstOrDefault();
                    if (ifcMaterialLayer == null) 
                        ifcMaterialLayer = Model.Instances.New<IfcMaterialLayer>(ml => { ml.Material = ifcMaterial; ml.LayerThickness = matThick; });
                } 
            }
        }

        /// <summary>
        /// Add the data to the Type object
        /// </summary>
        /// <param name="row">COBieTypeRow holding the data</param>
        private void AddType(COBieTypeRow row)
        {
            //we are merging so check for an existing item name, assume the same item as should be the same building
            if (CheckIfExistOnMerge<IfcTypeObject>(row.Name))
            {
                return;//we have it so no need to create
            }

            var ifcTypeObject = GetTypeInstance(row.ExtObject, Model);

            if (ifcTypeObject != null)
            {
                //Add Created By, Created On and ExtSystem to Owner History. 
                SetUserHistory(ifcTypeObject, row.ExtSystem, row.CreatedBy, row.CreatedOn);
            
                //using statement will set the Model.OwnerHistoryAddObject to ifcTypeObject.OwnerHistory as OwnerHistoryAddObject is used upon any property changes, 
                //then swaps the original OwnerHistoryAddObject back in the dispose, so set any properties within the using statement
                using (var context = new COBieXBimEditScope(Model, ifcTypeObject.OwnerHistory))
                {
                    var name = row.Name;
                    //Add Name
                    if (ValidateString(row.Name)) ifcTypeObject.Name = row.Name;

                    //Add Category
                    AddCategory(row.Category, ifcTypeObject);

                    //Add GlobalId
                    AddGlobalId(row.ExtIdentifier, ifcTypeObject);

                    //Add Description
                    if (ValidateString(row.Description)) ifcTypeObject.Description = row.Description;

                    if (ValidateString(row.AssetType))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Asset", "Type Asset Fixed or Movable Properties From COBie", "AssetAccountingType", "Asset Type Fixed or Movable", new IfcLabel(row.AssetType));
                    if (ValidateString(row.Manufacturer))
                        AddPropertySingleValue(ifcTypeObject, "Pset_ManufacturersTypeInformation", "Manufacturers Properties From COBie", "Manufacturer", "Manufacturer Contact for " + name, new IfcLabel(row.Manufacturer));
                    if (ValidateString(row.ModelNumber))
                        AddPropertySingleValue(ifcTypeObject, "Pset_ManufacturersTypeInformation", null, "ModelLabel", "Model Number for " + name, new IfcLabel(row.ModelNumber));
                    //reset property set name from "Pset_Warranty" via v16 matrix sheet to to "COBie_Warranty"
                    if (ValidateString(row.WarrantyGuarantorParts))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Warranty", "Warranty Information", "WarrantyGuarantorParts", "Warranty Contact for " + name, new IfcLabel(row.WarrantyGuarantorParts));
                    if (ValidateString(row.WarrantyDurationParts))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Warranty", null, "WarrantyDurationParts", "Warranty length for " + name, new IfcLabel(row.WarrantyDurationParts));
                    if (ValidateString(row.WarrantyGuarantorLabor))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Warranty", null, "WarrantyGuarantorLabor", "Warranty Labour Contact for " + name, new IfcLabel(row.WarrantyGuarantorLabor));
                    if (ValidateString(row.WarrantyDescription))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Warranty", null, "WarrantyDescription", "Warranty Description for" + name, new IfcLabel(row.WarrantyDescription));

                    if (ValidateString(row.WarrantyDurationLabor))
                    {
                        var ifcPropertySingleValue = AddPropertySingleValue(ifcTypeObject, "COBie_Warranty", null, "WarrantyDurationLabor", "Labour Warranty length for " + name, new IfcLabel(row.WarrantyDurationLabor));
                        //WarrantyDurationUnit
                        if (ValidateString(row.WarrantyDurationUnit))
                            ifcPropertySingleValue.Unit = GetDurationUnit(row.WarrantyDurationUnit);
                    }

                    if (ValidateString(row.ReplacementCost))
                    {
                        var value = GetDoubleFromString(row.ReplacementCost);
                        if (value != null)
                            AddPropertySingleValue(ifcTypeObject, "Pset_EconomicImpactValues", "Economic Impact Values", "ReplacementCost", "Replacement Cost for" + name, new IfcReal((double)value));
                    }
                    if (ValidateString(row.ExpectedLife))
                    {
                        var ifcPropertySingleValue = AddPropertySingleValue(ifcTypeObject, "Pset_ServiceLife", "Service Life", "ServiceLifeDuration", "Service Life length for " + name, new IfcLabel(row.ExpectedLife));
                        if (ValidateString(row.DurationUnit))
                            ifcPropertySingleValue.Unit = GetDurationUnit(row.DurationUnit);
                    }
                    //changed from "Pset_Specification" via v16 matrix sheet to "COBie_Specification"
                    if (ValidateString(row.NominalLength))
                    {
                        var value = GetDoubleFromString(row.NominalLength);
                        if (value != null)
                            AddPropertySingleValue(ifcTypeObject, "COBie_Specification", "Specification Properties", "NominalLength", "Nominal Length Value for " + name, new IfcReal((double)value));
                    }
                    if (ValidateString(row.NominalWidth))
                    {
                        var value = GetDoubleFromString(row.NominalWidth);
                        if (value != null)
                            AddPropertySingleValue(ifcTypeObject, "COBie_Specification", null, "NominalWidth", "Nominal Width Value for " + name, new IfcReal((double)value));
                    }
                    if (ValidateString(row.NominalHeight))
                    {
                        var value = GetDoubleFromString(row.NominalHeight);
                        if (value != null)
                            AddPropertySingleValue(ifcTypeObject, "COBie_Specification", null, "NominalHeight", "Nominal Height Value for " + name, new IfcReal((double)value));
                    }

                    if (ValidateString(row.ModelReference))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Specification", null, "ModelReference", "Model Reference Value for " + name, new IfcLabel(row.ModelReference));

                    if (ValidateString(row.Shape))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Specification", null, "Shape", "Shape Value for " + name, new IfcLabel(row.Shape));

                    if (ValidateString(row.Size))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Specification", null, "Size", "Size Value for " + name, new IfcLabel(row.Size));

                    if (ValidateString(row.Color))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Specification", null, "Color", "Color Value for " + name, new IfcLabel(row.Color));

                    if (ValidateString(row.Finish))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Specification", null, "Finish", "Finish Value for " + name, new IfcLabel(row.Finish));

                    if (ValidateString(row.Grade))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Specification", null, "Grade", "Grade Value for " + name, new IfcLabel(row.Grade));

                    if (ValidateString(row.Material))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Specification", null, "Material", "Material Value for " + name, new IfcLabel(row.Material));

                    if (ValidateString(row.Constituents))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Specification", null, "Constituents", "Constituents Value for " + name, new IfcLabel(row.Constituents));

                    if (ValidateString(row.Features))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Specification", null, "Features", "Features Value for " + name, new IfcLabel(row.Features));

                    if (ValidateString(row.AccessibilityPerformance))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Specification", null, "AccessibilityPerformance", "Accessibility Performance Value for " + name, new IfcLabel(row.AccessibilityPerformance));

                    if (ValidateString(row.CodePerformance))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Specification", null, "CodePerformance", "Code Performance Value for " + name, new IfcLabel(row.CodePerformance));

                    if (ValidateString(row.SustainabilityPerformance))
                        AddPropertySingleValue(ifcTypeObject, "COBie_Specification", null, "SustainabilityPerformance", "Sustainability Performance Value for " + name, new IfcLabel(row.SustainabilityPerformance));


                }
            }
            else
            {
#if DEBUG
                Console.WriteLine("Failed to create type {0} of {1}", row.Name, row.ExtObject);
#endif
            }


        }

        /// <summary>
        /// Create an Instance of a Object Type
        /// </summary>
        /// <param name="typeName">string name to create instance of</param>
        /// <param name="model">Model object</param>
        /// <returns>IfcTypeObject object of the type passed in of IfcTypeObject if failed to create passed in type</returns>
        public static IfcTypeObject GetTypeInstance(string typeName, IModel model)
        {
            typeName = typeName.Trim().ToUpper();
            
            ExpressType ifcType;
            IfcTypeObject ifcTypeObject = null;
            if (model.Metadata.TryGetExpressType(typeName, out ifcType))
            {
                var method = typeof(IEntityCollection).GetMethod("New", Type.EmptyTypes);
                var generic = method.MakeGenericMethod(ifcType.Type);
                var newObj = generic.Invoke(model.Instances, null);
                if (newObj is IfcTypeObject)
                    ifcTypeObject = (IfcTypeObject)newObj;
            }
            if (ifcTypeObject == null) //if we cannot make a object assume base IfcTypeObject
                ifcTypeObject = model.Instances.New<IfcTypeObject>();
            return ifcTypeObject;
        }

        
        #endregion

    }
}
