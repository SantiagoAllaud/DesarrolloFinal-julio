import { RestService, Rest } from '@abp/ng.core';
import type { PagedAndSortedResultRequestDto, PagedResultDto } from '@abp/ng.core';
import { Injectable, inject } from '@angular/core';
import type { CitySearchRequestDto, CitySearchResultDto, CreateUpdateDestinationDto, DestinationDto } from '../application/contracts/destinations/models';

@Injectable({
  providedIn: 'root',
})
export class DestinationService {
  private restService = inject(RestService);
  apiName = 'Default';
  

  create = (input: CreateUpdateDestinationDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, DestinationDto>({
      method: 'POST',
      url: '/api/app/destination',
      body: input,
    },
    { apiName: this.apiName,...config });
  

  delete = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, void>({
      method: 'DELETE',
      url: `/api/app/destination/${id}`,
    },
    { apiName: this.apiName,...config });
  

  get = (id: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, DestinationDto>({
      method: 'GET',
      url: `/api/app/destination/${id}`,
    },
    { apiName: this.apiName,...config });
  

  getList = (input: PagedAndSortedResultRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, PagedResultDto<DestinationDto>>({
      method: 'GET',
      url: '/api/app/destination',
      params: { sorting: input.sorting, skipCount: input.skipCount, maxResultCount: input.maxResultCount },
    },
    { apiName: this.apiName,...config });
  

  searchCities = (request: CitySearchRequestDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, CitySearchResultDto>({
      method: 'POST',
      url: '/api/app/destination/search-cities',
      body: request,
    },
    { apiName: this.apiName,...config });
  

  update = (id: string, input: CreateUpdateDestinationDto, config?: Partial<Rest.Config>) =>
    this.restService.request<any, DestinationDto>({
      method: 'PUT',
      url: `/api/app/destination/${id}`,
      body: input,
    },
    { apiName: this.apiName,...config });

  getEvents = (city: string, config?: Partial<Rest.Config>) =>
    this.restService.request<any, any[]>({
      method: 'GET',
      url: '/api/app/destination/events',
      params: { city },
    },
    { apiName: this.apiName,...config });
}