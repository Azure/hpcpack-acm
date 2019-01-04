import { Component, OnInit, ViewChild, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { MatDialogRef } from '@angular/material';
import { ApiService } from '../../services/api.service';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'diagnostics-tests',
  templateUrl: './new-diagnostics.component.html',
  styleUrls: ['./new-diagnostics.component.scss']
})
export class NewDiagnosticsComponent implements OnInit, OnDestroy {
  @ViewChild('tree') tree;

  public tests = [];
  private nodeFilter: string = '';
  public selectedDescription: string;
  private testInfoLink: string;
  public errorMessage: string;
  public diagTestName: string;
  public selectedTest: any;
  private selectedTestWithParameters: any;
  private paraForm: FormGroup;
  private ctlConfig = {};

  constructor(
    private api: ApiService,
    public dialogRef: MatDialogRef<NewDiagnosticsComponent>,
    private cdRef: ChangeDetectorRef,
    private fb: FormBuilder,
    private authService: AuthService
  ) {
  }


  ngOnInit() {
    this.api.diag.getDiagTests().subscribe(tests => {
      this.tests = tests.treeData;
      tests['rawData'].forEach(t => {
        if (t.parameters) {
          this.ctlConfig[t.category + t.name] = {};
          t.parameters.forEach(p => {
            let validators = [];
            if (p.required) {
              validators.push(Validators.required);
            }
            if (p.type == 'number') {
              validators.push(Validators.min(p.min));
              validators.push(Validators.max(p.max));
            }
            this.ctlConfig[t.category + t.name][p.name] = [p.defaultValue, validators];
          });
        }
      });
    });
  }

  ngAfterViewChecked() {
    this.cdRef.detectChanges();
  }

  onTreeLoad(tree) {
    tree.treeModel.expandAll();
  }

  public check() {
    this.selectedDescription = '';
    this.testInfoLink = '';
    this.selectedDescription = this.selectedTest.description;
    if (this.selectedDescription) {
      let index = this.selectedDescription.indexOf('http');
      if (index != -1) {
        this.testInfoLink = this.selectedDescription.substr(index);
        this.selectedDescription = this.selectedDescription.substr(0, index);
      }
    }
    this.updateCheckedNode();
  }

  private _sub: Subscription[];
  private updateHints(hint, paras, targetParam) {
    let index = paras.findIndex((data) => {
      return data.name == targetParam;
    });
    paras[index].description = hint;
  }

  getHint(param) {
    let type = typeof (param.description);
    if (type == 'string') {
      return param.description;
    }
    else {
      let option = this.paraForm.controls[param.name].value;
      return param.description[option];
    }
  }

  private updateCheckedNode() {
    this._sub = new Array<Subscription>();
    let paras = this.selectedTest.parameters;
    if (paras) {
      this.paraForm = this.fb.group(this.ctlConfig[this.selectedTest.category + this.selectedTest.name]);
      for (let i = 0; i < paras.length; i++) {
        let sub = this.paraForm.controls[paras[i].name].valueChanges.subscribe(data => {
          if (paras[i].whenChanged != undefined) {
            let selected = paras[i].whenChanged[data];
            for (let key in selected) {
              this.paraForm.controls[key].setValue(selected[key].value);
              if (selected[key].description) {
                this.updateHints(selected[key].description, paras, key);
              }
            }
          }
        });
        this._sub.push(sub);
      }
    }
    this.diagTestName = `${this.selectedTest.name} created by ${this.authService.username}`;
  }

  ngOnDestroy() {
    if (this._sub) {
      this._sub.forEach(sub => {
        sub.unsubscribe();
      })
    }
  }

  selectionChange(e) {
    if (e.selectedIndex == 2) {
      if (this.selectedTest != undefined) {
        this.diagTestName = `${this.selectedTest.name} created by ${this.authService.username}`;
      }
      else {
        this.diagTestName = `created by ${this.authService.username}`;
      }
    }
  }

  getTest() {
    if (this.selectedTest == undefined) {
      this.errorMessage = 'Please select one test to run in step 1 !';
    }
    else if (this.diagTestName == undefined || this.diagTestName == "") {
      this.errorMessage = 'Please enter the diagnostic name in step 3 ! ';
    }
    else {
      if (this.selectedTest.parameters != undefined) {
        let paras = this.selectedTest.parameters;
        let args = [];
        for (let i = 0; i < paras.length; i++) {
          if (this.paraForm.controls[paras[i].name].invalid) {
            this.errorMessage = 'Please enter valid parameters in step 2 !';
            return;
          }
          args.push({ name: paras[i].name, value: this.paraForm.controls[paras[i].name].value });
        }
        this.selectedTest.arguments = args;
      }
      let diagInfo = { selectedTest: this.selectedTest, diagTestName: this.diagTestName };
      this.dialogRef.close(diagInfo);
    }
  }

  clearErrorMsg() {
    this.errorMessage = "";
  }

  close() {
    this.dialogRef.close();
  }
}
