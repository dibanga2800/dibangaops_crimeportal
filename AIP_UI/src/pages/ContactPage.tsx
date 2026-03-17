import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useState, useRef } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
	Form,
	FormControl,
	FormField,
	FormItem,
	FormLabel,
	FormMessage,
} from '@/components/ui/form'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useToast } from '@/hooks/use-toast'
import { submitContactForm } from '@/services/contactService'
import { Mail, Loader2, Paperclip } from 'lucide-react'

const contactFormSchema = z.object({
	name: z.string().min(1, 'Name is required').min(2, 'Name must be at least 2 characters'),
	email: z.string().min(1, 'Email is required').email('Invalid email address'),
	jobRole: z.string().min(1, 'Job role is required'),
	description: z.string().min(10, 'Description must be at least 10 characters'),
	attachment: z
		.instanceof(File)
		.optional()
		.refine((file) => !file || file.size <= 5 * 1024 * 1024, 'File must be 5MB or less')
		.refine(
			(file) =>
				!file ||
				[
					'image/jpeg',
					'image/png',
					'image/gif',
					'image/webp',
					'application/pdf',
					'image/svg+xml',
					'text/plain',
				].includes(file.type),
			'Allowed: JPEG, PNG, GIF, WebP, PDF, SVG, TXT'
		),
})

type ContactFormValues = z.infer<typeof contactFormSchema>

const ContactPage = () => {
	const { toast } = useToast()
	const [isSubmitting, setIsSubmitting] = useState(false)
	const fileInputRef = useRef<HTMLInputElement>(null)

	const form = useForm<ContactFormValues>({
		resolver: zodResolver(contactFormSchema),
		defaultValues: {
			name: '',
			email: '',
			jobRole: '',
			description: '',
			attachment: undefined,
		},
	})

	const handleSubmit = async (values: ContactFormValues) => {
		setIsSubmitting(true)
		try {
			await submitContactForm({
				name: values.name,
				email: values.email,
				jobRole: values.jobRole,
				description: values.description,
				attachment: values.attachment,
			})
			toast({
				title: 'Message sent',
				description: 'Your message has been submitted successfully. We will get back to you soon.',
			})
			form.reset()
			if (fileInputRef.current) {
				fileInputRef.current.value = ''
			}
		} catch (error: unknown) {
			const message = error instanceof Error ? error.message : 'Failed to submit. Please try again.'
			toast({
				title: 'Submission failed',
				description: message,
				variant: 'destructive',
			})
		} finally {
			setIsSubmitting(false)
		}
	}

	return (
		<div className="w-full max-w-2xl mx-auto">
			<Card>
				<CardHeader>
					<CardTitle className="flex items-center gap-2 text-lg sm:text-xl">
						<Mail className="h-5 w-5" aria-hidden />
						Contact Us
					</CardTitle>
					<p className="text-sm text-muted-foreground">
						Send us a message or report an issue. Screenshots and documents can be attached.
					</p>
				</CardHeader>
				<CardContent>
					<Form {...form}>
						<form
							onSubmit={form.handleSubmit(handleSubmit)}
							className="space-y-4"
							noValidate
						>
							<FormField
								control={form.control}
								name="name"
								render={({ field }) => (
									<FormItem>
										<FormLabel>Name</FormLabel>
										<FormControl>
											<Input
												{...field}
												placeholder="Your name"
												autoComplete="name"
												disabled={isSubmitting}
												aria-invalid={!!form.formState.errors.name}
											/>
										</FormControl>
										<FormMessage />
									</FormItem>
								)}
							/>
							<FormField
								control={form.control}
								name="email"
								render={({ field }) => (
									<FormItem>
										<FormLabel>Email</FormLabel>
										<FormControl>
											<Input
												{...field}
												type="email"
												placeholder="your.email@example.com"
												autoComplete="email"
												disabled={isSubmitting}
												aria-invalid={!!form.formState.errors.email}
											/>
										</FormControl>
										<FormMessage />
									</FormItem>
								)}
							/>
							<FormField
								control={form.control}
								name="jobRole"
								render={({ field }) => (
									<FormItem>
										<FormLabel>Job role</FormLabel>
										<FormControl>
											<Input
												{...field}
												placeholder="e.g. Security Officer, Manager"
												autoComplete="organization-title"
												disabled={isSubmitting}
												aria-invalid={!!form.formState.errors.jobRole}
											/>
										</FormControl>
										<FormMessage />
									</FormItem>
								)}
							/>
							<FormField
								control={form.control}
								name="description"
								render={({ field }) => (
									<FormItem>
										<FormLabel>Description</FormLabel>
										<FormControl>
											<Textarea
												{...field}
												placeholder="Describe your issue, question or feedback..."
												rows={4}
												disabled={isSubmitting}
												aria-invalid={!!form.formState.errors.description}
												className="resize-y min-h-[100px]"
											/>
										</FormControl>
										<FormMessage />
									</FormItem>
								)}
							/>
							<FormField
								control={form.control}
								name="attachment"
								render={({ field: { onChange, value, ...rest } }) => (
									<FormItem>
										<FormLabel>Attachment (optional)</FormLabel>
										<FormControl>
											<div className="flex flex-col sm:flex-row gap-2 items-start">
												<Input
													{...rest}
													ref={fileInputRef}
													type="file"
													accept=".jpg,.jpeg,.png,.gif,.webp,.pdf,.svg,.txt"
													disabled={isSubmitting}
													aria-label="Upload file (screenshots, errors)"
													onChange={(e) => {
														const file = e.target.files?.[0]
														onChange(file ?? undefined)
													}}
													className="cursor-pointer file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-medium file:bg-primary file:text-primary-foreground hover:file:bg-primary/90"
												/>
												{value && (
													<span className="text-sm text-muted-foreground flex items-center gap-1">
														<Paperclip className="h-4 w-4" aria-hidden />
														{value.name}
													</span>
												)}
											</div>
										</FormControl>
										<FormMessage />
										<p className="text-xs text-muted-foreground">
											Screenshots, error reports, logs. Max 5MB. Allowed: JPEG, PNG, GIF, WebP, PDF, SVG, TXT
										</p>
									</FormItem>
								)}
							/>
							<div className="pt-2">
								<Button type="submit" disabled={isSubmitting} className="w-full sm:w-auto">
									{isSubmitting ? (
										<>
											<Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden />
											Sending...
										</>
									) : (
										'Send message'
									)}
								</Button>
							</div>
						</form>
					</Form>
				</CardContent>
			</Card>
		</div>
	)
}

export default ContactPage
