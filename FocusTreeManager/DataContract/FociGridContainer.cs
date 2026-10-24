using FocusTreeManager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using FocusTreeManager.Model.TabModels;

namespace FocusTreeManager.DataContract
{
    [KnownType(typeof(Focus))]
    [KnownType(typeof(FileInfo))]
    [DataContract(Name = "foci_container", Namespace = "focusesNs")]
    public class FociGridContainer
    {
        [DataMember(Name = "guid_id", Order = 0)]
        public Guid IdentifierID { get; private set; }

        [DataMember(Name = "text_id", Order = 1)]
        public string ContainerID { get; set; }

        [DataMember(Name = "tag", Order = 2)]
        public string TAG { get; set; }

        [DataMember(Name = "foci", Order = 3)]
        public List<Focus> FociList { get; set; }

        [DataMember(Name = "mods", Order = 4)]
        public string AdditionnalMods { get; set; }

        [DataMember(Name = "file", Order = 5)]
        public FileInfo FileInfo { get; set; }

        public FociGridContainer()
        {
            IdentifierID = Guid.NewGuid();
            FociList = new List<Focus>();
        }

        public FociGridContainer(string filename)
        {
            ContainerID = filename;
            FociList = new List<Focus>();
            IdentifierID = Guid.NewGuid();
        }

        public FociGridContainer(FocusGridModel item)
        {
            IdentifierID = item.UniqueID;
            ContainerID = item.VisibleName;
            TAG = item.TAG;
            AdditionnalMods = item.AdditionnalMods;
            FileInfo = item.FileInfo;
            FociList = new List<Focus>();
            foreach (FocusModel model in item.FociList)
            {
                FociList.Add(new Focus
                {
                    UniqueName = model.UniqueName,
                    Text = model.Text,
                    Image = model.Image,
                    X = model.X,
                    Y = model.Y,
                    Cost = model.Cost,
                    InternalScript = model.InternalScript,
                    Note = model.Note
                });
            }
            //Repair sets and relative to
            //(lookups are built once, FirstOrDefault for every reference made saving quadratic)
            NameLookup<FocusModel> models = new NameLookup<FocusModel>(item.FociList, 
                f => f.UniqueName);
            NameLookup<Focus> foci = new NameLookup<Focus>(FociList, f => f.UniqueName);
            foreach (Focus focus in FociList)
            {
                FocusModel associatedModel = models.Find(focus.UniqueName);
                //If no model was found, continue the loop, the focus is broken
                if (associatedModel == null) continue;
                //Relative to
                if (associatedModel.CoordinatesRelativeTo != null)
                {
                    focus.RelativeTo = foci.Find(
                        associatedModel.CoordinatesRelativeTo.UniqueName);
                }
                //Prerequisites
                foreach (PrerequisitesSetModel set in associatedModel.Prerequisite)
                {
                    PrerequisitesSet newset = new PrerequisitesSet(focus);
                    foreach (FocusModel model in set.FociList)
                    {
                        newset.FociList.Add(foci.Find(model.UniqueName));
                    }
                    focus.Prerequisite.Add(newset);
                }
                //Mutually exclusives
                foreach (MutuallyExclusiveSetModel set in associatedModel.MutualyExclusive)
                {
                    MutuallyExclusiveSet newset = new MutuallyExclusiveSet(
                        foci.Find(set.Focus1.UniqueName),
                        foci.Find(set.Focus2.UniqueName));
                    focus.MutualyExclusive.Add(newset);
                }
            }
        }
    }
}
