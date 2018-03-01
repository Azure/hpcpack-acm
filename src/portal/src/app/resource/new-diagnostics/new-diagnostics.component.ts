import { Component, OnInit, ViewChild, Inject } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { TreeComponent } from 'angular-tree-component';
import { NodeFilterBuilderComponent } from '../../widgets/node-filter-builder/node-filter-builder.component';

@Component({
  selector: 'diagnostics-tests',
  templateUrl: './new-diagnostics.component.html',
  styleUrls: ['./new-diagnostics.component.css']
})
export class NewDiagnosticsComponent implements OnInit {
  @ViewChild(TreeComponent)
  private tree: TreeComponent;

  private tests = [{
    name: 'All',
    children: [
      {
        name: 'SOA',
        children: [
          {
            name: 'SOA Service Loading',
            description: 'Verify that SOA service is OK.',
            parameters: [
              {
                name: 'Service Name',
                defaultValue: 'CcpEchoSvc',
              }
            ],
          }
        ]
      },
      {
        name: 'MPI',
        children: [
          {
            name: 'MPI Latency',
            description: 'Measure network Latency between each pair of nodes.',
            parameters: [
              {
                name: 'Network',
                options: ['Default', 'Private', 'Enterprise', 'Application'],
                defaultValue: 'Default',
              },
              {
                name: 'Running Mode',
                options: ['ring', 'serial', 'tournament'],
                defaultValue: 'tournament',
              },
            ],
          },
          {
            name: 'MPI Throughput',
            description: 'Measure Throughput among nodes. May take a long time.',
          },
          {
            name: 'MPI Simple Throughput',
            description: 'Measure Throughput only between pairs of adjacent nodes.',
          }
        ]
      },
    ]
  }];

  private selectedTests = [];
  private selectedTestsWithParameters = []
  private nodeFilter: string = '';

  constructor(
    public dialogRef: MatDialogRef<NewDiagnosticsComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {}

  ngOnInit() {
    setTimeout(() => this.tree.treeModel.expandAll(), 0);
  }

  private check(node, checked) {
    this.updateChildNodeCheckbox(node, checked);
    this.updateParentNodeCheckbox(node.realParent);
  }

  private updateChildNodeCheckbox(node, checked) {
    node.data.checked = checked;
    if (node.children) {
      node.children.forEach((child) => this.updateChildNodeCheckbox(child, checked));
    }
  }

  private updateParentNodeCheckbox(node) {
    if (!node) {
      return;
    }

    let allChildrenChecked = true;
    let noChildChecked = true;

    for (const child of node.children) {
      if (!child.data.checked || child.data.indeterminate) {
        allChildrenChecked = false;
      }
      if (child.data.checked) {
        noChildChecked = false;
      }
    }

    if (allChildrenChecked) {
      node.data.checked = true;
      node.data.indeterminate = false;
    }
    else if (noChildChecked) {
      node.data.checked = false;
      node.data.indeterminate = false;
    }
    else {
      node.data.checked = true;
      node.data.indeterminate = true;
    }
    this.updateParentNodeCheckbox(node.parent);
  }

  private getSelectedTests(node: any): string[] {
    if (!node.checked) {
      return [];
    }
    let array = [];
    if (node.children) {
      for (let i = 0; i < node.children.length; i++) {
        array = array.concat(this.getSelectedTests(node.children[i]));
      }
    }
    return array.length > 0 ? array : [node];
  }

  private selectTests(): void {
    this.selectedTests = this.getSelectedTests(this.tests[0]);
    this.selectedTestsWithParameters = this.selectedTests.filter(test => test.parameters);
  }
}
