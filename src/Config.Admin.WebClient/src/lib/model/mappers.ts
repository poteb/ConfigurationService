import type {
	ApiConfiguration,
	ApiConfigurationHeader,
	ApiSecret,
	ApiSecretHeader
} from '$lib/api/adminApi';
import { newId, type Header, type NamedItem, type Section } from './types';

/**
 * The API stores a section's applications/environments as an embedded JSON
 * string, serialized by the Blazor client with .NET's default (PascalCase)
 * property names. We keep that exact wire shape for compatibility.
 */
type WireNamedItem = { Id?: string; Name?: string; IsDeleted?: boolean; IsSelected?: boolean };

export function namedItemsFromString(itemsJson: string | null | undefined): NamedItem[] {
	if (!itemsJson) return [];
	try {
		const parsed = JSON.parse(itemsJson) as WireNamedItem[];
		if (!Array.isArray(parsed)) return [];
		return parsed.map((item) => ({
			id: item.Id ?? '',
			name: item.Name ?? '',
			isDeleted: item.IsDeleted ?? false,
			isSelected: item.IsSelected ?? false
		}));
	} catch {
		return [];
	}
}

export function namedItemsToString(items: NamedItem[]): string {
	return JSON.stringify(
		items.map((item) => ({
			Id: item.id,
			Name: item.name,
			IsDeleted: item.isDeleted,
			IsSelected: item.isSelected
		}))
	);
}

function sectionFromApiConfiguration(api: ApiConfiguration): Section {
	return {
		headerId: api.headerId ?? '',
		id: api.id ?? '',
		json: api.json ?? '',
		value: '',
		valueType: '',
		createdUtc: api.createdUtc ?? '',
		isActive: api.isActive ?? true,
		deleted: api.deleted ?? false,
		isJsonEncrypted: api.isJsonEncrypted ?? false,
		applications: namedItemsFromString(api.applications),
		environments: namedItemsFromString(api.environments),
		index: api.index ?? 0,
		isNew: false
	};
}

function sectionToApiConfiguration(section: Section): ApiConfiguration {
	return {
		headerId: section.headerId,
		id: section.id,
		json: section.json,
		createdUtc: section.createdUtc,
		isActive: section.isActive,
		deleted: section.deleted,
		isJsonEncrypted: section.isJsonEncrypted,
		applications: namedItemsToString(section.applications),
		environments: namedItemsToString(section.environments),
		index: section.index
	};
}

function sectionFromApiSecret(api: ApiSecret): Section {
	return {
		headerId: api.headerId ?? '',
		id: api.id ?? '',
		json: '',
		value: api.value ?? '',
		valueType: api.valueType ?? '',
		createdUtc: api.createdUtc ?? '',
		isActive: api.isActive ?? true,
		deleted: api.deleted ?? false,
		isJsonEncrypted: false,
		applications: namedItemsFromString(api.applications),
		environments: namedItemsFromString(api.environments),
		index: 0,
		isNew: false
	};
}

function sectionToApiSecret(section: Section): ApiSecret {
	return {
		headerId: section.headerId,
		id: section.id,
		value: section.value,
		valueType: section.valueType,
		createdUtc: section.createdUtc,
		isActive: section.isActive,
		deleted: section.deleted,
		applications: namedItemsToString(section.applications),
		environments: namedItemsToString(section.environments)
	};
}

export function configurationHeaderToClient(api: ApiConfigurationHeader): Header {
	return {
		id: api.id ?? '',
		name: api.name ?? '',
		createdUtc: api.createdUtc ?? '',
		updateUtc: api.updateUtc ?? '',
		deleted: api.deleted ?? false,
		isActive: api.isActive ?? true,
		isJsonEncrypted: api.isJsonEncrypted ?? false,
		sections: (api.configurations ?? []).map(sectionFromApiConfiguration)
	};
}

export function configurationHeaderToApi(header: Header): ApiConfigurationHeader {
	return {
		id: header.id,
		name: header.name.trim(),
		createdUtc: header.createdUtc,
		updateUtc: header.updateUtc,
		deleted: header.deleted,
		isActive: header.isActive,
		isJsonEncrypted: header.isJsonEncrypted,
		configurations: header.sections.map(sectionToApiConfiguration)
	};
}

export function secretHeaderToClient(api: ApiSecretHeader): Header {
	return {
		id: api.id ?? '',
		name: api.name ?? '',
		createdUtc: api.createdUtc ?? '',
		updateUtc: api.updateUtc ?? '',
		deleted: api.deleted ?? false,
		isActive: api.isActive ?? true,
		isJsonEncrypted: false,
		sections: (api.secrets ?? []).map(sectionFromApiSecret)
	};
}

export function secretHeaderToApi(header: Header): ApiSecretHeader {
	return {
		id: header.id,
		name: header.name.trim(),
		createdUtc: header.createdUtc,
		updateUtc: header.updateUtc,
		deleted: header.deleted,
		isActive: header.isActive,
		secrets: header.sections.map(sectionToApiSecret)
	};
}

/** Deep copy used for the dirty baseline and for undo. */
export function cloneHeader(header: Header): Header {
	return structuredClone(header);
}

/**
 * Copy of a section for duplication: new id, never deleted, same content.
 * (Port of ConfigurationMapper.Copy with generateNewId: true.)
 */
export function copySection(section: Section, generateNewId = false): Section {
	return {
		...structuredClone(section),
		id: generateNewId ? newId() : section.id,
		deleted: false
	};
}

/** Copy of a whole header for the Duplicate action: new ids throughout. */
export function copyHeader(header: Header, generateNewId = false): Header {
	const id = generateNewId ? newId() : header.id;
	return {
		...structuredClone(header),
		id,
		sections: header.sections.map((s) => ({ ...copySection(s, generateNewId), headerId: id }))
	};
}
