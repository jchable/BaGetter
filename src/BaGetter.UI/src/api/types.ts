// --- Config ---
export interface AppConfig {
  serviceIndexUrl: string;
  publishUrl: string;
  symbolPublishUrl: string;
  appVersion: string;
  statisticsEnabled: boolean;
  oauthProviders: string[];
}

// --- Auth ---
export interface User {
  id: string;
  userName: string;
  email: string;
  displayName: string;
  roles: string[];
  tenantId: string | null;
}

// --- Search (reuses NuGet v3 protocol types) ---
export interface SearchResponse {
  totalHits: number;
  data: SearchResult[];
}

export interface SearchResult {
  id: string;
  version: string;
  description: string;
  authors: string[];
  iconUrl: string;
  totalDownloads: number;
  versions: SearchResultVersion[];
  tags: string[];
  packageTypes: PackageType[];
}

export interface SearchResultVersion {
  version: string;
  downloads: number;
}

export interface PackageType {
  name: string;
}

// --- Package Detail ---
export interface PackageDetail {
  id: string;
  version: string;
  description: string;
  authors: string[];
  iconUrl: string | null;
  licenseUrl: string | null;
  projectUrl: string | null;
  repositoryUrl: string | null;
  repositoryType: string | null;
  tags: string[];
  totalDownloads: number;
  downloads: number;
  published: string;
  lastUpdated: string;
  listed: boolean;
  hasReadme: boolean;
  readme: string | null;
  releaseNotes: string | null;
  packageDownloadUrl: string;
  deprecation: PackageDeprecation | null;
  dependencyGroups: DependencyGroup[];
  versions: VersionInfo[];
  packageTypes: PackageType[];
  usedBy: PackageDependent[];
}

export interface PackageDeprecation {
  reasons: string[];
  message: string | null;
  alternatePackage: {
    id: string;
    range: string;
  } | null;
}

export interface DependencyGroup {
  name: string;
  dependencies: Dependency[];
}

export interface Dependency {
  packageId: string;
  versionSpec: string;
}

export interface VersionInfo {
  version: string;
  downloads: number;
  lastUpdated: string;
  selected: boolean;
}

export interface PackageDependent {
  id: string;
  description: string;
  totalDownloads: number;
}

// --- Admin ---
export interface DashboardStats {
  userCount: number;
  packageCount: number;
  pendingInvitationCount: number;
  totalDownloads: number;
}

export interface AdminUser {
  id: string;
  userName: string;
  email: string;
  displayName: string;
  role: string;
  tenantId: string | null;
  createdAt: string;
}

export interface Invitation {
  id: number;
  email: string;
  role: string;
  invitedById: string;
  token: string;
  expiresAt: string;
  acceptedAt: string | null;
}

export interface ApiKey {
  id: number;
  name: string;
  keyPrefix: string;
  userId: string;
  role: string;
  createdAt: string;
  expiresAt: string | null;
  lastUsedAt: string | null;
  isRevoked: boolean;
}

export interface AuditLogEntry {
  id: string;
  action: string;
  userId: string;
  userName: string;
  details: string;
  timestamp: string;
  ipAddress: string;
}

export interface Tenant {
  id: string;
  name: string;
  slug: string;
  userCount: number;
  packageCount: number;
  createdAt: string;
}

// --- Import ---
export interface ImportProgress {
  status: string;
  packagesImported: number;
  totalPackages: number;
  currentPackage: string | null;
  errorMessage: string | null;
}
