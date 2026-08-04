import { describe, expect, it } from 'vitest';
import { generateApiKey } from './apikeys';

describe('generateApiKey', () => {
	it('produces csk_-prefixed keys without +, / or =', () => {
		for (let i = 0; i < 20; i++) {
			const key = generateApiKey();
			expect(key.startsWith('csk_')).toBe(true);
			expect(key).not.toMatch(/[+/=]/);
			// 32 bytes base64 ≈ 43 chars minus stripped ones.
			expect(key.length).toBeGreaterThan(30);
		}
	});

	it('produces unique keys', () => {
		expect(generateApiKey()).not.toBe(generateApiKey());
	});
});
