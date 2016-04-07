﻿using DevExpress.EntityFramework.SecurityDataStore;
using EFCoreSecurityODataService.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;

namespace EFCoreSecurityODataService.Controllers {
    public class ContactsController : ODataController {
        EFCoreDemoDbContext dbContext = new EFCoreDemoDbContext();
        public ContactsController() {
            SecuritySetup();
            dbContext.SaveChanges();
        }
        private void SecuritySetup() {
            dbContext.Security.AddMemberPermission<EFCoreDemoDbContext, Contact>(SecurityOperation.Read, OperationState.Deny, "Address", (db, obj) => obj.Name == "John");
            dbContext.Security.AddObjectPermission<EFCoreDemoDbContext, Contact>(SecurityOperation.Read, OperationState.Deny, (db, obj) => obj.Department.Title == "Sales");
            dbContext.Security.AddObjectPermission<EFCoreDemoDbContext, Contact>(SecurityOperation.Read, OperationState.Deny, (db, obj) => obj.ContactTasks.Any(p => p.Task.Description == "Draw"));
        }
        private bool ContactExists(int key) {
            return dbContext.Contacts.Any(p => p.Id == key);
        }
        protected override void Dispose(bool disposing) {
            dbContext.Dispose();
            base.Dispose(disposing);
        }
        [EnableQuery]
        public IQueryable<Contact> Get() {
            IQueryable<Contact> result = dbContext.Contacts
                .Include(p => p.Department)
                .Include(c => c.ContactTasks)
                .ThenInclude(ct => ct.Task);
            return result;
        }
        [EnableQuery]
        public SingleResult<Contact> Get([FromODataUri] int key) {
            IQueryable<Contact> result = dbContext.Contacts.Where(p => p.Id == key).Include(p => p.Department).Include(c => c.ContactTasks).ThenInclude(ct => ct.Task);
            return SingleResult.Create(result);
        }
        public async Task<IHttpActionResult> Post(Contact contact) {
            if(!ModelState.IsValid) {
                return BadRequest(ModelState);
            }
            dbContext.Contacts.Add(contact);
            await dbContext.SaveChangesAsync();
            return Created(contact);
        }
        public async Task<IHttpActionResult> Patch([FromODataUri] int key, Delta<Contact> contact) {
            if(!ModelState.IsValid) {
                return BadRequest(ModelState);
            }
            var entity = await dbContext.Contacts.FirstOrDefaultAsync(p => p.Id == key);
            if(entity == null) {
                return NotFound();
            }
            contact.Patch(entity);
            try {
                await dbContext.SaveChangesAsync();
            }
            catch(DbUpdateConcurrencyException) {
                if(!ContactExists(key)) {
                    return NotFound();
                }
                else {
                    throw;
                }
            }
            return Updated(contact);
        }
        public async Task<IHttpActionResult> Put([FromODataUri] int key, Contact contact) {
            if(!ModelState.IsValid) {
                return BadRequest(ModelState);
            }
            if(key != contact.Id) {
                return BadRequest();
            }
            dbContext.Entry(contact).State = EntityState.Modified;
            try {
                await dbContext.SaveChangesAsync();
            }
            catch(DbUpdateConcurrencyException) {
                if(!ContactExists(key)) {
                    return NotFound();
                }
                else {
                    throw; 
                }
            }
            return Updated(contact);
        }
        public async Task<IHttpActionResult> Delete([FromODataUri] int key) {
            var contact = await dbContext.Contacts.FirstOrDefaultAsync(p => p.Id == key);
            if(contact == null) {
                return NotFound();
            }
            dbContext.Contacts.Remove(contact);
            await dbContext.SaveChangesAsync();
            return StatusCode(HttpStatusCode.NoContent);
        }
        [EnableQuery]
        public SingleResult<Department> GetDepartment([FromODataUri] int key) {
            IQueryable<Department> result = dbContext.Contacts.Where(p => p.Id == key).Select(m => m.Department);
            return SingleResult.Create(result);
        }
        //[EnableQuery]
        //public SingleResult<Contact> GetManager([FromODataUri] int key) {
        //    IQueryable<Contact> result = dbContext.Contacts.Where(p => p.Id == key).Select(m => m.Manager);
        //    return SingleResult.Create(result);
        //}
        //[EnableQuery]
        //public SingleResult<Position> GetPosition([FromODataUri] int key) {
        //    IQueryable<Position> result = dbContext.Contacts.Where(p => p.Id == key).Select(m => m.Position);
        //    return SingleResult.Create(result);
        //}
        [EnableQuery]
        public IQueryable<ContactTask> GetContactTasks([FromODataUri] int key) {
            IQueryable<ContactTask> result = dbContext.Contacts.Where(p => p.Id == key).SelectMany(p => p.ContactTasks);
            return result;
        }
        [AcceptVerbs("POST", "PUT")]
        public async Task<IHttpActionResult> CreateRef([FromODataUri] int key, string navigationProperty, [FromBody] Uri link) {
            Contact contact = await dbContext.Contacts.SingleOrDefaultAsync(p => p.Id == key);
            if(contact == null) {
                return NotFound();
            }
            switch(navigationProperty) {
                case "Department":
                    int relatedKey = Helpers.GetKeyFromUri<int>(Request, link);
                    Department department = await dbContext.Departments.SingleOrDefaultAsync(p => p.Id == relatedKey);
                    if(department == null) {
                        return NotFound();
                    }
                    contact.Department = department;
                    break;
                //case "ContactTasks":
                //    relatedKey = Helpers.GetKeyFromUri<int>(Request, link);
                //    ContactTask task = await dbContext.ContactTasks.SingleOrDefaultAsync(p => p.Id == relatedKey);
                //    if(task == null) {
                //        return NotFound();
                //    }
                //    contact.ContactTasks.Add(task);
                //    break;
                //case "Position":
                //    relatedKey = Helpers.GetKeyFromUri<int>(Request, link);
                //    Position position = await dbContext.Positions.SingleOrDefaultAsync(p => p.Id == relatedKey);
                //    if(position == null) {
                //        return NotFound();
                //    }
                //    contact.Position = position;
                //    break;
                //case "Manager":
                //    relatedKey = Helpers.GetKeyFromUri<int>(Request, link);
                //    Contact manager = await dbContext.Contacts.SingleOrDefaultAsync(p => p.Id == relatedKey);
                //    if(manager == null) {
                //        return NotFound();
                //    }
                //    contact.Manager = manager;
                //    break;
                default:
                    return StatusCode(HttpStatusCode.NotImplemented);
            }
            await dbContext.SaveChangesAsync();
            return StatusCode(HttpStatusCode.NoContent);
        }
        public async Task<IHttpActionResult> DeleteRef([FromODataUri] int key, string navigationProperty, [FromBody] Uri link) {
            Contact contact = await dbContext.Contacts.SingleOrDefaultAsync(p => p.Id == key);
            if(contact == null) {
                return NotFound();
            }
            switch(navigationProperty) {
                case "Department":
                    contact.Department = null;
                    break;
                //case "Position":
                //    contact.Position = null;
                //    break;
                //case "Manager":
                //    contact.Manager = null;
                //    break;
                default:
                    return StatusCode(HttpStatusCode.NotImplemented);
            }
            await dbContext.SaveChangesAsync();
            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}