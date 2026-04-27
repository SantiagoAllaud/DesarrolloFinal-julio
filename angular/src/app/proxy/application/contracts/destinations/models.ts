import type { AuditedEntityDto } from '@abp/ng.core';

export interface CityDto {
  id?: string;
  name?: string;
  country?: string;
  countryCode?: string;
  region?: string;
  regionCode?: string;
  latitude?: string;
  longitude?: string;
  population?: number;
}

export interface CitySearchRequestDto {
  partialName?: string;
  limit: number;
  countryCode?: string;
  regionCode?: string;
  minPopulation?: number;
}

export interface CitySearchResultDto {
  cities: CityDto[];
}

export interface CreateUpdateDestinationDto {
  name?: string;
  country?: string;
  city?: string;
  photoUrl?: string;
  latitude?: string;
  longitude?: string;
}

export interface DestinationDto extends AuditedEntityDto<string> {
  name?: string;
  country?: string;
  city?: string;
  photoUrl?: string;
  lastUpdated?: string;
  latitude?: string;
  longitude?: string;
  averageRating?: number;
  ratingCount?: number;
}

export interface DestinationRatingDto {
  id: string;
  userId: string;
  destinationId: string;
  score: number;
  comment?: string;
  creationTime: string;
}

export interface DestinationEventDto {
  name?: string;
  date?: string;
  time?: string;
  url?: string;
  imageUrl?: string;
  venueName?: string;
}
