import { Component, OnInit } from '@angular/core';
import {Http, Response, Headers} from '@angular/http';


@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  display='none';
  constructor(private http: Http) { }

  /*
    1. iniate the customers array
    2. data is fetched from local json server with data storage
    3. results stored in the customers array
    4. function called 

  */
  customers = [];
  fetchData = function() {
   this.http.get("http://localhost:5555/customers").subscribe(
     (res: Response) => {
       this.customers = res.json();
     }
   )

  }

  /* 
   1. Create object
   2. Create function to read form data
  */
  customerObj:object = {};

  addNewCustomer = function(customer) {
    this.customerObj = {
      "firstname": customer.firstname,
      "lastname": customer.lastname
    }
    this.http.post("http://localhost:5555/customers", this.customerObj).subscribe((res:Response) => {
      console.log(res);
        this.fetchData();
        this.display='none';
        
    })
  }

  onCloseHandled(){
    this.display='none';
  }

  openModal(){
    this.display='block';
  }
  

  ngOnInit() {
    this.fetchData();
  }

}
