/** Client-side mirror of the server's PasswordPolicy (the server is authoritative). */
export function validatePassword(password: string): string | null {
	if (password.length < 16) return 'Password must be at least 16 characters.';
	if (password.length > 128) return 'Password must be at most 128 characters.';
	if (!/[a-z]/.test(password)) return 'Password must contain a lowercase letter.';
	if (!/[A-Z]/.test(password)) return 'Password must contain an uppercase letter.';
	if (!/[0-9]/.test(password)) return 'Password must contain a digit.';
	if (!/[^a-zA-Z0-9]/.test(password)) return 'Password must contain a special character.';
	return null;
}
