export type EntityKind = 'configuration' | 'secret';

/** An application or environment as selected on a section. */
export type NamedItem = {
	id: string;
	name: string;
	isDeleted: boolean;
	isSelected: boolean;
};

/**
 * One section of a header. Configurations use `json` + `isJsonEncrypted`;
 * secrets use `value` + `valueType`. The unused fields stay at their defaults.
 */
export type Section = {
	headerId: string;
	id: string;
	json: string;
	value: string;
	valueType: string;
	createdUtc: string;
	isActive: boolean;
	deleted: boolean;
	isJsonEncrypted: boolean;
	environments: NamedItem[];
	applications: NamedItem[];
	index: number;
	isNew: boolean;
};

/** A configuration or secret header with its ordered sections. */
export type Header = {
	id: string;
	name: string;
	createdUtc: string;
	updateUtc: string;
	deleted: boolean;
	isActive: boolean;
	isJsonEncrypted: boolean;
	sections: Section[];
};

export function newId(): string {
	return crypto.randomUUID();
}

export function newSection(headerId: string, index: number): Section {
	return {
		headerId,
		id: newId(),
		json: '',
		value: '',
		valueType: '',
		createdUtc: new Date().toISOString(),
		isActive: true,
		deleted: false,
		isJsonEncrypted: false,
		environments: [],
		applications: [],
		index,
		isNew: true
	};
}

export function newHeader(): Header {
	const id = newId();
	return {
		id,
		name: '',
		createdUtc: new Date().toISOString(),
		updateUtc: new Date().toISOString(),
		deleted: false,
		isActive: true,
		isJsonEncrypted: false,
		sections: [newSection(id, 0)]
	};
}
