using Starcounter;
using System;
using System.Diagnostics;

namespace KitchenSink
{
    [Database]
    public class Person
    {
        public string FirstName;
        public string LastName;
        public long Ord; //to set order of displayed records
    }
    
    partial class SortableListPage : Json
    {   
        protected override void OnData()
        {
            base.OnData();            
            RefreshPersons();
        }

        public void RefreshPersons()
        {
            //this.Persons = Db.SQL("SELECT p FROM KitchenSink.Person p ORDER BY p.Ord");
            this.Persons.Clear();
            SortableListPage.PersonItem person = null;

            QueryResultRows<Person> persons = Db.SQL<Person>("SELECT p FROM KitchenSink.Person p ORDER BY p.Ord");
            int i = 0;
            foreach (var p in persons)
            {
                person = this.Persons.Add();
                person.FirstName = p.FirstName;
                person.LastName = p.LastName;
                person.Ord = p.Ord;
                person.First = (++i == 1);
                person.Last = false;
            }
            if(person != null) person.Last = true;
        }

        public void ReorderUp(long ord)
        {
            Person currPerson = Db.SQL<Person>("SELECT p FROM KitchenSink.Person p WHERE p.Ord = ?", ord).First;
            var persons = Db.SQL<Person>("SELECT p FROM KitchenSink.Person p WHERE p.Ord < ? ORDER BY p.Ord desc", ord);
            if (persons != null)
            {
                Person prevPerson = persons.First;
                if (prevPerson != null)
                {
                    Db.Transact(() =>
                    {
                        long tmpOrd = currPerson.Ord;
                        currPerson.Ord = prevPerson.Ord;
                        prevPerson.Ord = tmpOrd;
                    });
                    RefreshPersons();
                }
            }
        }

        public void ReorderDown(long ord)
        {
            Person currPerson = Db.SQL<Person>("SELECT p FROM KitchenSink.Person p WHERE p.Ord = ?", ord).First;
            var persons = Db.SQL<Person>("SELECT p FROM KitchenSink.Person p WHERE p.Ord > ? ORDER BY p.Ord", ord);
            if (persons != null)
            {
                Person nextPerson = persons.First;
                if (nextPerson != null)
                {
                    Db.Transact(() =>
                    {
                        long tmpOrd = currPerson.Ord;
                        currPerson.Ord = nextPerson.Ord;
                        nextPerson.Ord = tmpOrd;
                    });
                    RefreshPersons();
                }
            }
        }

        public void Reorder(long ord1, long ord2)
        {
            Debug.Print(ord1.ToString() + " na " + ord2.ToString());
            if (ord1 != ord2)
            {                
                Person p1 = Db.SQL<Person>("SELECT p FROM KitchenSink.Person p WHERE p.Ord = ?", ord1).First;
                Person p2 = Db.SQL<Person>("SELECT p FROM KitchenSink.Person p WHERE p.Ord = ?", ord2).First;
                if(p1 != null && p2 != null)
                {
                    Db.Transact(() =>
                    {                        
                        p1.Ord = ord2;
                        p2.Ord = ord1;
                    });
                    RefreshPersons();
                }
            }
        }
        [SortableListPage_json.Persons]
        partial class PersonItem : Json, IBound<Person>
        {            
            void Handle(Input.Up action)
            {                
                this.ParentPage.ReorderUp(this.Ord); 
            }

            void Handle(Input.Down action)
            {
                this.ParentPage.ReorderDown(this.Ord);
            }

            public SortableListPage ParentPage
            {
                get
                {
                    return this.Parent.Parent as SortableListPage;
                }
            }

            void Handle(Input.DropTrigger action)
            {                
                this.ParentPage.Reorder(Math.Abs(action.Value), Math.Abs(this.Ord));
            }
        }
        
    }
}
