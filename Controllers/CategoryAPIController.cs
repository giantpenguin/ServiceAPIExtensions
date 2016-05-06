using EPiServer.DataAbstraction;
using EPiServer.ServiceApi.Configuration;
using EPiServer.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace ServiceAPIExtensions.Controllers
{
    [RoutePrefix("episerverapi/category")]
    public class CategoryAPIController : ApiController
    {
        private CategoryRepository _catRepo = ServiceLocator.Current.GetInstance<CategoryRepository>();

        [HttpGet]
        [Route("{parent?}")]
        public IHttpActionResult List(string parent = "")
        {
            var parentCat = string.IsNullOrWhiteSpace(parent) ? _catRepo.GetRoot() : _catRepo.Get(parent);
            if (parentCat == null)
            {
                return NotFound();
            }

            return Ok(parentCat.Categories.Select<Category, object>(cat => TransformCategory(cat)));
        }

        [HttpPost]
        [Route("{parentId:int}")]
        //[AuthorizePermission("EPiServerServiceApi", "WriteAccess")]
        public IHttpActionResult Create(int parentId, [FromBody] ExpandoObject cat)
        {
            var parentCat = _catRepo.Get(parentId);
            if (parentCat == null)
            {
                return NotFound();
            }

            var props = cat as IDictionary<string, object>;
            var catName = props.ContainsKey("Name") ? (string)props["Name"] : string.Empty;
            if (string.IsNullOrWhiteSpace(catName))
            {
                return InternalServerError(new ArgumentException("Category name is required."));
            }

            var newCat = _catRepo.Get(catName);
            if (newCat != null)
            {
                return InternalServerError(new ArgumentException(string.Format("Category {0} is already existed.", catName)));
            }

            newCat = new Category(parentCat, catName);
            newCat.Description = props.ContainsKey("Description") ? (string)props["Description"] : string.Empty;

            object propVal; bool bval; int ival;
            if (props.TryGetValue("SortOrder", out propVal) && int.TryParse(propVal.ToString(), out ival))
            {
                newCat.SortOrder = ival;
            }
            if (props.TryGetValue("Selectable", out propVal) && bool.TryParse(propVal.ToString(), out bval))
            {
                newCat.Selectable = bval;
            }
            if (props.TryGetValue("Available", out propVal) && bool.TryParse(propVal.ToString(), out bval))
            {
                newCat.Available = bval;
            }

            _catRepo.Save(newCat);

            return Ok(newCat.ID);
        }

        [HttpPut]
        [Route("update")]
        //[AuthorizePermission("EPiServerServiceApi", "WriteAccess")]
        public IHttpActionResult Update([FromBody] ExpandoObject updated)
        {
            object propVal; int catId; Category currentCat = null;
            var props = updated as IDictionary<string, object>;
            // try get existing Category for update
            if (props.TryGetValue("Id", out propVal) && int.TryParse((string)propVal, out catId))
            {
                currentCat = _catRepo.Get(catId);
            }

            var catName = props.ContainsKey("Name") ? (string)props["Name"] : string.Empty;
            if (currentCat == null || !string.IsNullOrWhiteSpace(catName))
            {
                currentCat = _catRepo.Get(catName);
                if (currentCat == null)
                {
                    return InternalServerError(new ArgumentException("Category is not existed."));
                }
            }

            var catUpdating = currentCat.CreateWritableClone();

            // change Name if different
            if (!string.IsNullOrWhiteSpace(catName) && !currentCat.Name.Equals(catName, StringComparison.InvariantCulture))
            {
                catUpdating.Name = catName;
            }

            if (props.ContainsKey("Description"))
            {
                catUpdating.Description = (string)props["Description"]; 
            }

            bool bval; int ival;
            if (props.TryGetValue("SortOrder", out propVal) && int.TryParse(propVal.ToString(), out ival))
            {
                catUpdating.SortOrder = ival;
            }
            if (props.TryGetValue("Selectable", out propVal) && bool.TryParse(propVal.ToString(), out bval))
            {
                catUpdating.Selectable = bval;
            }
            if (props.TryGetValue("Available", out propVal) && bool.TryParse(propVal.ToString(), out bval))
            {
                catUpdating.Available = bval;
            }

            // change the parent bases on ParentId
            if (props.TryGetValue("ParentId", out propVal) && int.TryParse(propVal.ToString(), out ival))
            {
                var parentCat = _catRepo.Get(ival);
                if (parentCat != null && !parentCat.Categories.Any<Category>(c => c.Name.Equals(currentCat.Name, StringComparison.InvariantCulture)))
                {
                    catUpdating.Parent = parentCat;
                }
            }

            _catRepo.Save(catUpdating);

            return Ok(catUpdating.ID);
        }

        [HttpDelete]
        [Route("delete/{catNameOrId}")]
        //[AuthorizePermission("EPiServerServiceApi", "WriteAccess")]
        public IHttpActionResult Delete(string catNameOrId)
        { 
            var cat = _catRepo.Get(catNameOrId);
            if (cat == null)
            {
                int catId;
                if (int.TryParse(catNameOrId, out catId))
                {
                    cat = _catRepo.Get(catId);
                }

                if (cat == null)
                {
                    return NotFound();
                }
            }

            _catRepo.Delete(cat);
            return Ok();
        }

        public virtual ExpandoObject TransformCategory(Category cat)
        {
            dynamic dynObj = new ExpandoObject();
            //var dic = dynObj as IDictionary<string, object>;
            dynObj.Name = cat.Name;
            dynObj.Id = cat.ID;
            dynObj.Guid = cat.GUID;
            dynObj.Description = cat.Description;
            //dynObj.IsReadOnly = cat.IsReadOnly;
            dynObj.Selectable = cat.Selectable;
            dynObj.Available = cat.Available;
            dynObj.SortOrder = cat.SortOrder;
            dynObj.Indent = cat.Indent;

            return dynObj;
        }
    }
}
