export interface IViewSignCert {
  subject?: string;
  commonName?: string;
  issuer?: string;
  issuerCommonName?: string;
  serialNumber?: string;
  thumbprint?: string;
  source?: number;
  keyProvider?: string;
  validFrom?: string;
  validTo?: string;
  hasPrivateKey?: boolean;
  isExpired?: boolean;
  allowsSigning?: boolean;
  isTrusted?: boolean;
  canSign?: boolean;
  reason?: string | null;
}
