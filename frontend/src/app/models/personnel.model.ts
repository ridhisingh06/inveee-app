export interface PersonnelResponse {
  id: number;
  name: string;
  icNumber?: string;
  birthDate?: string;
  email: string;
  residentialAddress?: string;
  residentialPhone?: string;
  officePhone?: string;
  designation?: string;
  jobDescription?: string;
  department?: string;
  isStoresIncharge: boolean;
  building?: string;
  reportingOfficer?: string;
  idCardNumber?: string;
  idCardExpiryDate?: string;
  photoUrl?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface PersonnelPagedResult {
  data: PersonnelResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
