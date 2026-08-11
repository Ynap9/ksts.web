export interface IViewTrangThaiPlugin {
  ten?: string;
  phienBan?: string;
  sanSang?: boolean;
}

export interface IViewBoCaiPlugin {
  fileName?: string;
  exists?: boolean;
}

export interface IViewSignCert {
  subject?: string;
  commonName?: string;
  issuer?: string;
  issuerCommonName?: string;
  serialNumber?: string;
  thumbprint?: string;
  source?: number;
  keyProvider?: string | null;
  validFrom?: string;
  validTo?: string;
  hasPrivateKey?: boolean;
  isExpired?: boolean;
  allowsSigning?: boolean;
  reason?: string | null;
}

export interface IViewTokenVerify {
  thumbprint?: string;
  commonName?: string | null;
  foundInStore?: boolean;
  hasPrivateKey?: boolean;
  notExpired?: boolean;
  allowsSigning?: boolean;
  onUsbToken?: boolean;
  canSignTest?: boolean;
  valid?: boolean;
  reason?: string | null;
}

export interface IViewCertScanResult {
  certificates?: IViewSignCert[];
  storeDiagnostics?: string[];
}

export interface IChungThuSoDaChon {
  thumbprint?: string;
  commonName?: string;
  issuerCommonName?: string;
  validTo?: string;
}

export interface ICertRow extends IViewSignCert {
    sourceLabel: string;
    statusLabel: string;
    kyDuoc: boolean;
}