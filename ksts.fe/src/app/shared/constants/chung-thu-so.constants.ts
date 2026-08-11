export enum ECertSource {
  Local = 0,
  Server = 1,
  UsbToken = 2
}

export const CERT_SOURCE_LABELS: Record<ECertSource, string> = {
  [ECertSource.Local]: 'Máy cá nhân',
  [ECertSource.Server]: 'Máy chủ',
  [ECertSource.UsbToken]: 'USB token'
};

export const CERT_SOURCE_SEVERITIES: Record<ECertSource, 'success' | 'info' | 'secondary'> = {
  [ECertSource.Local]: 'info',
  [ECertSource.Server]: 'secondary',
  [ECertSource.UsbToken]: 'success'
};

export const CERT_SIGNABLE_LABEL = 'Ký được';
export const CERT_NOT_SIGNABLE_LABEL = 'Không ký được';

export const PLUGIN_PROBE_TIMEOUT_MS = 2000;
