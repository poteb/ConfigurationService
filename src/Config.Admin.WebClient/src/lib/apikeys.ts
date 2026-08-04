/** `csk_` + base64 of 32 cryptographically random bytes, stripped of +/=. */
export function generateApiKey(): string {
	const bytes = crypto.getRandomValues(new Uint8Array(32));
	let binary = '';
	for (const byte of bytes) binary += String.fromCharCode(byte);
	return 'csk_' + btoa(binary).replaceAll('+', '').replaceAll('/', '').replaceAll('=', '');
}
