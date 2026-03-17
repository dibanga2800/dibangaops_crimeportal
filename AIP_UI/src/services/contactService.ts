import { api, handleApiError } from '@/config/api'

const CONTACT_ENDPOINT = '/contact'

export interface ContactFormPayload {
	name: string
	email: string
	jobRole: string
	description: string
	attachment?: File
}

export interface ContactResponse {
	success: boolean
	message?: string
}

/**
 * Submits the contact form to the backend API.
 * Backend will send email to david.ibanga@advantage1.co.uk with cc: dibanga2800@gmail.com
 */
export const submitContactForm = async (payload: ContactFormPayload): Promise<ContactResponse> => {
	const formData = new FormData()
	formData.append('name', payload.name)
	formData.append('email', payload.email)
	formData.append('jobRole', payload.jobRole)
	formData.append('description', payload.description)
	if (payload.attachment) {
		formData.append('attachment', payload.attachment)
	}

	try {
		// Override default JSON Content-Type so the browser can set proper multipart boundaries
		const { data } = await api.post<ContactResponse>(CONTACT_ENDPOINT, formData, {
			headers: {
				'Content-Type': 'multipart/form-data',
				Accept: 'application/json',
			},
		})
		return data
	} catch (error) {
		throw new Error(handleApiError(error))
	}
}
