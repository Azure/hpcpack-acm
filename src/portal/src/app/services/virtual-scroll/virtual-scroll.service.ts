import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class VirtualScrollService {

  constructor() { }

  indexChangedCalc(pageSize, pivot, cdkVirtualScrollViewport, dataSource, lastScrolled, startIndex) {
    let range = cdkVirtualScrollViewport.getRenderedRange();
    let scrolled = cdkVirtualScrollViewport.getOffsetToRenderedContentStart();
    let direction: string;
    let lastId: number;
    let loading: boolean;
    let endId: number;
    let initialPivot = Math.round(pageSize / 2);
    // Add increasedIndex to update startIndex and pivot 
    let increasedIndex = Math.round(cdkVirtualScrollViewport.getViewportSize() / 40);
    let isScrolled: boolean;

    if (scrolled > lastScrolled) {
      direction = 'down';
    }
    else if (scrolled < lastScrolled) {
      direction = 'up';
    }

    let start = range.start;
    if (start != 0) {
      isScrolled = true;
    }
    else {
      isScrolled = false;
    }
    let end = range.end;
    let totalLen = dataSource.length;

    // if has data and the data in view is at the end of datasource, should load more data.
    // endId is used to determine if the new request finish and new data returned by comparing endId and data[data.length-].id
    if ((end - 1) >= 0 && (totalLen - 1) >= 0 && dataSource[end - 1].id == dataSource[totalLen - 1].id) {
      loading = true;
      endId = dataSource[totalLen - 1].id;
    }
    else {
      loading = false;
    }
    // startIndex records the updating data's first index, 
    // because the id is not successive in datasource, we use index to get id.
    // Pivot is the middle index of the updating data in datasource,
    // When rendered data's start index is larger or equal to pivot, which means that the request returned data has scrolled half,
    // we update the request range to get the newest data which id is from lastId to pageSize+lastId.
    // The update strategy is Simply discrad the updating data's first 1/3 data, add new 1/3 data at the end.
    // change lastId to update the request url

    // Scroll down logic
    if ((start >= pivot || end > (totalLen - increasedIndex)) && direction == 'down') {
      startIndex = (startIndex + increasedIndex) > totalLen ? startIndex : (startIndex + increasedIndex - 1);
      pivot = pivot + increasedIndex > totalLen ? pivot : (pivot + increasedIndex);
      lastId = dataSource[startIndex].id;
    }

    // Scroll up logic
    if ((end <= pivot || start < startIndex) && direction == 'up') {
      startIndex = (startIndex + 1 - increasedIndex) <= 0 ? 0 : (startIndex + 1 - increasedIndex);
      pivot = startIndex == 0 ? initialPivot : ((pivot - increasedIndex) < initialPivot) ? initialPivot : (pivot - increasedIndex);
      lastId = startIndex == 0 ? 0 : (dataSource[startIndex] ? dataSource[startIndex].id : 0);
    }

    return {
      lastId: lastId,
      loading: loading,
      lastScrolled: scrolled,
      startIndex: startIndex,
      pivot: pivot,
      endId: endId,
      scrolled: isScrolled
    };

  }
}
