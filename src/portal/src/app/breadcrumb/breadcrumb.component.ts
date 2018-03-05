import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, NavigationEnd, PRIMARY_OUTLET } from '@angular/router';
import { Subscription } from 'rxjs/Subscription';
import "rxjs/add/operator/filter";

@Component({
  selector: 'app-breadcrumb',
  templateUrl: './breadcrumb.component.html',
  styleUrls: ['./breadcrumb.component.css']
})
export class BreadcrumbComponent implements OnInit {
  private breadcrumbs = [];

  private subcription: Subscription;

  constructor(
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit() {
    this.subcription = this.router.events.filter(event => event instanceof NavigationEnd).subscribe(event => {
      let root = this.route.root;
      this.breadcrumbs = this.getBreadcrumbs(root);
    });
  }

  ngOnDestroy() {
    this.subcription.unsubscribe();
  }

  private getBreadcrumbs(route, url = "", breadcrumbs = []) {
    const ROUTE_DATA_BREADCRUMB: string = "breadcrumb";

    let children: ActivatedRoute[] = route.children;
    if (children.length === 0)
      return breadcrumbs;

    let child;
    for (child of children) {
      if (child.outlet === PRIMARY_OUTLET)
        break;
    }

    if (!child.snapshot.data.hasOwnProperty(ROUTE_DATA_BREADCRUMB))
      return this.getBreadcrumbs(child, url, breadcrumbs);

    let routeURL: string = child.snapshot.url.map(segment => segment.path).join("/");
    if (routeURL === '')
      return this.getBreadcrumbs(child, url, breadcrumbs);

    url += `/${routeURL}`;
    breadcrumbs.push({
      label: child.snapshot.data[ROUTE_DATA_BREADCRUMB],
      params: child.snapshot.params,
      url: url
    });

    return this.getBreadcrumbs(child, url, breadcrumbs);
  }
}
