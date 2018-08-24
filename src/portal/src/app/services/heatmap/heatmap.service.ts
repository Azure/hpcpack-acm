import { Injectable } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class HeatmapService {

  private colorMap = [{
    value: 0, color: 'ten'
  }, {
    value: 1, color: 'twenty'
  }, {
    value: 2, color: 'thirty'
  }, {
    value: 3, color: 'forty'
  }, {
    value: 4, color: 'fifty'
  }, {
    value: 5, color: 'sixty'
  }, {
    value: 6, color: 'seventy'
  }, {
    value: 7, color: 'eighty'
  }, {
    value: 8, color: 'ninety'
  }, {
    value: 9, color: 'full'
  }];

  nodeClass(node): string {
    let res;
    if (isNaN(node.value)) {
      return;
    }

    if (node.value == 0) {
      return res = 'empty';
    }
    if (node.value == 100) {
      return res = 'full';
    }
    let val = Math.floor(node.value / 10);
    let item = this.colorMap.find(item => {
      return item.value == val;
    })
    res = item.color;
    return res;
  }

  clickNode(node): void {
    this.router.navigate(['/resource', node.id]);
  }

  constructor(
    private router: Router
  ) { }
}
