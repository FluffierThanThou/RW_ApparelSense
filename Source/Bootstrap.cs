using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommunityCoreLibrary;
using RimWorld;
using Verse;

namespace Fluffy
{
    public class ApparelSense : SpecialInjector
    {
        // our root thingCategory def
        private ThingCategoryDef apparelRoot = ThingCategoryDefOf.Apparel;
        
        // create a category def and plop it into the defDB
        private ThingCategoryDef CreateCategory( string label, string type )
        {
            // create cat def
            ThingCategoryDef cat = new ThingCategoryDef();
            cat.parent = apparelRoot;
            cat.label = label;
            cat.defName = GetCatName( label, type );
            DefDatabase<ThingCategoryDef>.Add( cat );

            // don't forget to call the PostLoad() function, or you'll get swarmed in red... (ugh)
            cat.PostLoad();

            // update parent
            cat.parent.childCategories.Add( cat );

            // done!
            return cat;
        }

        // create a unique category name
        public string GetCatName( string label, string type )
        {
            return "ThingCategoryDef_Apparel_" + type + "_" + label;
        }

        // exact copy of Verse.ThingCategoryNodeDatabase.SetNestLevelRecursive (Tynan, pls).
        private static void SetNestLevelRecursive( TreeNode_ThingCategory node, int nestDepth )
        {
            foreach( ThingCategoryDef current in node.catDef.childCategories )
            {
                current.treeNode.nestDepth = nestDepth;
                SetNestLevelRecursive( current.treeNode, nestDepth + 1 );
            }
        }

        // gets called from CCL, when exactly is defined in ModHelperDef (should be right after game is loaded).
        public override void Inject()
        {
            // get a list of all apparel in the game
            List<ThingDef> allApparel = DefDatabase<ThingDef>.AllDefsListForReading.Where( t => t.IsApparel ).ToList();

            // detach all existing categories under apparel
            foreach ( ThingCategoryDef cat in apparelRoot.childCategories )
            {
                cat.parent = null;
            }
            apparelRoot.childCategories = new List<ThingCategoryDef>();

            // loop over all apparel, adding categories where appropriate.
            foreach ( ThingDef thing in allApparel )
            {
                // create list of categories on thing if necessary (shouldn't ever be, but what the heck)
                if( thing.thingCategories.NullOrEmpty() )
                {
                    thing.thingCategories = new List<ThingCategoryDef>();
                }

                // remove existing categories on thing
                foreach ( ThingCategoryDef cat in thing.thingCategories )
                {
                    cat.childThingDefs.Remove( thing );
                }
                
                // add in new categories
                ApparelProperties apparel = thing.apparel;

                // categories based on bodyparts
                foreach ( BodyPartGroupDef bodyPart in apparel.bodyPartGroups )
                {
                    // get or create category
                    ThingCategoryDef cat = DefDatabase<ThingCategoryDef>.GetNamedSilentFail( GetCatName( bodyPart.label, "BP" ) );
                    if ( cat == null )
                    {
                        cat = CreateCategory( bodyPart.label, "BP" );
                    }
                    
                    // add category to thing, and thing to category. (Tynan, pls.)
                    thing.thingCategories.Add( cat );
                    cat.childThingDefs.Add( thing );
                }

                //// categories based on tag (too messy)
                //foreach ( string tag in apparel.tags )
                //{
                //    // get or create category
                //    ThingCategoryDef cat = DefDatabase<ThingCategoryDef>.GetNamedSilentFail( GetCatName( tag, "BP" ) );
                //    if( cat == null )
                //    {
                //        cat = CreateCategory( tag, "BP" );
                //    }

                //    // add category to thing, and thing to category. (Tynan, pls.)
                //    thing.thingCategories.Add( cat );
                //    cat.childThingDefs.Add( thing );
                //}
            }

            // set nest levels on new categories
            SetNestLevelRecursive( apparelRoot.treeNode, apparelRoot.treeNode.nestDepth );
        }
    }
}
